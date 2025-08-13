import { test } from "node:test";
import { strict as assert } from "node:assert/strict";
import { SBBContent, SBBParser, SBBTag } from "..";

test("should parse correctly", (t) => {
  const p = new SBBParser();
  const actual = p.parse("[b]bold text[/b]");
  const expected = [
    new SBBTag('b', [], [
      new SBBContent('bold text')
    ])
  ];

  assert.deepEqual(actual, expected);
});
