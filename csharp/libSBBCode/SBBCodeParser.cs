using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using libSBBCode.Internal;

namespace libSBBCode;

public interface ISBBCodeParser
{
    List<ISBBElement> Parse(string code);
    List<ISBBElement> Parse(string code, IEnumerable<AllowedTag>? allowedTags);
}

public class SBBCodeParser : ISBBCodeParser
{
    public List<ISBBElement> Parse(string code)
    {
        return Parse(code, null);
    }

    public List<ISBBElement> Parse(string code, IEnumerable<AllowedTag>? allowedTags)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var foftw = new FailOnFirstWriteTextWriter(code);

        var inputStream = new AntlrInputStream(code);

        var lexer = new SBBCodeLexer(inputStream, foftw, foftw);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new Internal.SBBCodeParser(tokenStream, foftw, foftw);

        Internal.SBBCodeParser.ParseContext tree;
        try
        {
            tree = parser.parse() ?? throw new ArgumentException("unable to parse SBBCode input");
            if (parser.NumberOfSyntaxErrors > 0) throw new ArgumentException($"{parser.NumberOfSyntaxErrors} errors detected while parse SBBCode input");
        }
        catch (RecognitionException e)
        {
            throw new ArgumentException($"error in SBBCode input: {parser.GetErrorHeader(e)}", e);
        }

        Dictionary<string, AllowedTagLookup>? atp = null;
        if (allowedTags != null)
            atp = allowedTags
                .ToDictionary(
                    k => k.Name,
                    v => new AllowedTagLookup(
                        v.Name,
                        v.Attributes.ToDictionary(
                            ak => ak.Name,
                            av => av
                        ),
                        v.ExtraAttributes)
                );

        var rv = walk_tree(tree, atp);
        return rv;
    }

    private static List<ISBBElement> walk_tree(ParserRuleContext tree, Dictionary<string, AllowedTagLookup>? allowedTags)
    {
        if (tree is Internal.SBBCodeParser.ParseContext pCtx)
        {
            var rv = new List<ISBBElement>(pCtx.ChildCount);
            foreach (var prc in pCtx.children.OfType<ParserRuleContext>())
            {
                var cnv = walk_tree(prc, allowedTags);
                rv.AddRange(cnv);
            }

            return rv;
        }

        if (tree is Internal.SBBCodeParser.ElementContext eCtx)
        {
            var cCtx = eCtx.content();
            if (cCtx != null)
            {
                var text = cCtx.value.Text ?? throw new ArgumentException();
                return [new SBBContent(text)];
            }

            var tCtx = eCtx.tag();
            if (tCtx != null)
            {
                var to = tCtx.tag_open();
                var tc = tCtx.tag_close();
                if (to.name.Text != tc.name.Text) throw new InvalidOperationException($"opened tag [{to.name.Text}] cant be closed with [/{tc.name.Text}]");

                AllowedTagLookup? allowedTag = null;
                if (allowedTags != null && !allowedTags.TryGetValue(to.name.Text, out allowedTag)) throw new ArgumentException($"tag [{to.name.Text}] not allowed");

                var tagAttributes = SBBTag.EmptyAttr;
                if (to.attribute()?.Length > 0)
                {
                    tagAttributes = to.attribute()
                        .Select(aCtx =>
                        {
                            if (allowedTag != null)
                            {
                                if (allowedTag.Attributes.TryGetValue(aCtx.name.Text, out var allowedTagAttribute))
                                    switch (aCtx.value.Type)
                                    {
                                        case Internal.SBBCodeParser.DQSTRING:
                                        case Internal.SBBCodeParser.QSTRING:
                                            if (!allowedTagAttribute.ValueTypes.Contains(typeof(string)))
                                                throw new ArgumentException($"tag [{allowedTag.Name}] contain attribute {aCtx.name.Text} with not allowed type 'string'");
                                            break;
                                        case Internal.SBBCodeParser.INTNUMBER:
                                            if (!(allowedTagAttribute.ValueTypes.Contains(typeof(int))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(long))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(short))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(uint))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(ulong))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(byte))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(sbyte))
                                                )
                                               )
                                                throw new ArgumentException($"tag [{allowedTag.Name}] contain attribute {aCtx.name.Text} with not allowed type 'intnumber'");
                                            break;
                                        case Internal.SBBCodeParser.FLOATNUMBER:
                                            if (!(allowedTagAttribute.ValueTypes.Contains(typeof(double))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(decimal))
                                                  || allowedTagAttribute.ValueTypes.Contains(typeof(float))
                                                )
                                               )
                                                throw new ArgumentException($"tag [{allowedTag.Name}] contain attribute {aCtx.name.Text} with not allowed type 'floatnumber'");
                                            break;
                                        case Internal.SBBCodeParser.TRUE:
                                        case Internal.SBBCodeParser.FALSE:
                                            if (!allowedTagAttribute.ValueTypes.Contains(typeof(bool)))
                                                throw new ArgumentException($"tag [{allowedTag.Name}] contain attribute {aCtx.name.Text} with not allowed type 'string'");
                                            break;
                                        default:
                                            throw new InvalidOperationException();
                                    }
                                else if (!allowedTag.ExtraAttributes) throw new ArgumentException($"tag [{allowedTag.Name}] contains not allowed extra attribute {aCtx.name.Text}");
                            }

                            var rv = aCtx.value.Type switch
                            {
                                Internal.SBBCodeParser.DQSTRING => (ISBBTagAttribute)new SBBTagStringAttribute(aCtx.name.Text, aCtx.value.Text.Trim(['"'])),
                                Internal.SBBCodeParser.QSTRING => new SBBTagStringAttribute(aCtx.name.Text, aCtx.value.Text.Trim(['\''])),
                                Internal.SBBCodeParser.INTNUMBER => new SBBTagIntAttribute(aCtx.name.Text, int.Parse(aCtx.value.Text, NumberStyles.None | NumberStyles.AllowLeadingSign)),
                                Internal.SBBCodeParser.FLOATNUMBER => new SBBTagFloatAttribute(aCtx.name.Text, double.Parse(aCtx.value.Text, CultureInfo.InvariantCulture)),
                                Internal.SBBCodeParser.TRUE => new SBBTagBoolAttribute(aCtx.name.Text, true),
                                Internal.SBBCodeParser.FALSE => new SBBTagBoolAttribute(aCtx.name.Text, false),
                                _ => throw new InvalidOperationException()
                            };


                            return rv;
                        })
                        .ToList();

                    if (allowedTag != null)
                    {
                    }
                }

                var tagElements = SBBTag.EmptyElements;
                if (tCtx.element()?.Length > 0)
                {
                    tagElements = new List<ISBBElement>();
                    foreach (var els in tCtx.element().Select(e => walk_tree(e, allowedTags))) tagElements.AddRange(els);
                }

                var tag = new SBBTag(to.name.Text, tagAttributes, tagElements);
                return [tag];
            }

            throw new InvalidOperationException("ElementContext must have content or tag");
        }


        throw new NotImplementedException();
    }

    private class FailOnFirstWriteTextWriter(string _code) : TextWriter
    {
        public override Encoding Encoding { get; } = Encoding.UTF8;

        #region Write* operations

        public override void Write(string? value)
        {
            throw new ArgumentException();
        }

        public override void Write(bool value)
        {
            throw new ArgumentException();
        }

        public override void Write(char value)
        {
            throw new ArgumentException();
        }

        public override void Write(char[]? buffer)
        {
            throw new ArgumentException();
        }

        public override void Write(char[] buffer, int index, int count)
        {
            throw new ArgumentException();
        }

        public override void Write(decimal value)
        {
            throw new ArgumentException();
        }

        public override void Write(double value)
        {
            throw new ArgumentException();
        }

        public override void Write(int value)
        {
            throw new ArgumentException();
        }

        public override void Write(long value)
        {
            throw new ArgumentException();
        }

        public override void Write(object? value)
        {
            throw new ArgumentException();
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            throw new ArgumentException();
        }

        public override void Write(float value)
        {
            throw new ArgumentException();
        }

        public override void Write(string format, object? arg0)
        {
            throw new ArgumentException();
        }

        public override void Write(string format, object? arg0, object? arg1)
        {
            throw new ArgumentException();
        }

        public override void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            throw new ArgumentException();
        }

        public override void Write(string format, params object?[] arg)
        {
            throw new ArgumentException();
        }

        public override void Write(StringBuilder? value)
        {
            throw new ArgumentException();
        }

        public override void Write(uint value)
        {
            throw new ArgumentException();
        }

        public override void Write(ulong value)
        {
            throw new ArgumentException();
        }

        public override Task WriteAsync(char value)
        {
            throw new ArgumentException();
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            throw new ArgumentException();
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
        {
            throw new ArgumentException();
        }

        public override Task WriteAsync(string? value)
        {
            throw new ArgumentException();
        }

        public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new())
        {
            throw new ArgumentException();
        }

        public override void WriteLine()
        {
            throw new ArgumentException();
        }

        public override void WriteLine(bool value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(char value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(char[]? buffer)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(decimal value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(double value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(int value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(long value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(object? value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(float value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(string? value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(string format, object? arg0)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(string format, object? arg0, object? arg1)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(string format, params object?[] arg)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(StringBuilder? value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(uint value)
        {
            throw new ArgumentException();
        }

        public override void WriteLine(ulong value)
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync()
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync(char value)
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync(string? value)
        {
            throw new ArgumentException();
        }

        public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new())
        {
            throw new ArgumentException();
        }

        #endregion
    }

    private record AllowedTagLookup(string Name, Dictionary<string, AllowedTagAttribute> Attributes, bool ExtraAttributes);
}