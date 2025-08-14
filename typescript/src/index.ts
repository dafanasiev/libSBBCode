import { CharStreams, CommonTokenStream, ParserRuleContext } from "antlr4";
import InternalSBBCodeParser, {
  ElementContext,
  ParseContext,
} from "./internal/SBBCodeParser";
import SBBCodeLexer from "./internal/SBBCodeLexer";

export interface ISBBElement {}

export class SBBTagAttribute {
  constructor(private _name: string, private _value: unknown) {}
  get value(): unknown {
    return this._value;
  }
  get name(): string {
    return this._name;
  }
}

export class SBBTag implements ISBBElement {
  constructor(
    private _name: string,
    private _attributes: Array<SBBTagAttribute>,
    private _elements: Array<ISBBElement>
  ) {}

  get attributes(): Array<SBBTagAttribute> {
    return this._attributes;
  }

  get name(): string {
    return this._name;
  }

  get elements(): Array<ISBBElement> {
    return this._elements;
  }
}

export class SBBContent implements ISBBElement {
  constructor(private _value: string) {}

  get value(): string {
    return this._value;
  }
}

export class SBBCodeParser {
  public parse(text: string): Array<ISBBElement> {
    const stream = CharStreams.fromString(text);
    const lexer = new SBBCodeLexer(stream);

    const tokenStream = new CommonTokenStream(lexer);
    const parser = new InternalSBBCodeParser(tokenStream);

    const tree = parser.parse();
    if (parser.syntaxErrorsCount != 0) {
      throw new Error(
        `unable to parse input: ${parser.syntaxErrorsCount} error(s) detected`
      );
    }

    return this.walk_tree(tree);
  }

  private walk_tree(tree: ParserRuleContext): Array<ISBBElement> {
    if (tree instanceof ParseContext) {
      const pCtx = tree as ParseContext;
      const rv: Array<ISBBElement> = [];
      for (let prc of pCtx.children ?? []) {
        if (prc instanceof ParserRuleContext) {
          const cnv = this.walk_tree(prc);
          rv.push(...cnv)
        }
      }
      return rv;
    }

    if (tree instanceof ElementContext) {
      const eCtx = tree as ElementContext;
      const cCtx = eCtx.content();
      if (cCtx) {
        const text = cCtx.TEXT().getText();
        if (!text) {
          throw new Error("ElementContext must have text");
        }
        return [new SBBContent(text)];
      }

      const tCtx = eCtx.tag();
      if (tCtx) {
        const to = tCtx.tag_open();
        var tc = tCtx.tag_close();
        if (to._name.text != tc._name.text) {
          throw new Error(
            `opened tag [${to._name.text}] cant be closed with [/${tc._name.text}]`
          );
        }

        const tagAttributes: Array<SBBTagAttribute> = to
          .attribute_list()
          .map((a) => {
            const aname = a._name.text;
            let avalue: unknown = undefined;
            switch (a._value.type) {
              case InternalSBBCodeParser.DQSTRING:
              case InternalSBBCodeParser.QSTRING:
                avalue = a._value.text.slice(1, a._value.text.length - 2);
                break;
              case SBBCodeLexer.INTNUMBER:
                avalue = parseInt(a._value.text, 10);
                break;
              case SBBCodeLexer.FLOATNUMBER:
                avalue = parseFloat(a._value.text);
                break;
              case SBBCodeLexer.TRUE:
                avalue = true;
                break;
              case SBBCodeLexer.FALSE:
                avalue = false;
                break;
              default:
                throw new Error("unsipported tag attribute value type");
            }

            return new SBBTagAttribute(aname, avalue);
          });

        const tagElements: Array<ISBBElement> = tCtx
          .element_list()
          .flatMap((e) => {
            return this.walk_tree(e);
          });

        return [new SBBTag(to._name.text, tagAttributes, tagElements)];
      }
    }

    throw new Error("Unsupported type of AST node");
  }
}
