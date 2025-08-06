namespace libSBBCode.Tests;

public class ParserTests
{
    [Theory]
    [MemberData(nameof(SBBCodeBadMessages))]
    public void BadMessages(string m)
    {
        ISBBCodeParser p = new SBBCodeParser();
        Assert.ThrowsAny<Exception>(()=> p.Parse(m));
    }    
    
    [Theory]
    [MemberData(nameof(SBBCodeGoodMessages))]
    public void GoodMessages(string m, List<ISBBElement> expected)
    {
        ISBBCodeParser p = new SBBCodeParser();
        var actual = p.Parse(m);
        Assert.NotEmpty(actual);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(SBBCodeGoodMessagesWithChecks))]
    public void GoodMessagesWithChecks(string m,
        IEnumerable<AllowedTag> allowedTags,
        List<ISBBElement> expected)
    {
        ISBBCodeParser p = new SBBCodeParser();
        var actual = p.Parse(m, allowedTags);
        Assert.NotEmpty(actual);
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> SBBCodeBadMessages =
    [
        [
            "[unclosed tag input"
        ],
        [
            "[b]unmatched tag[/i]"
        ],
        [
            "[b][i]unclosed b tag[/i]"
        ],
        [
            "[i][b]unmatched tags[/i][/b]"
        ],
        [
            "]unmatched tags"
        ],
        [
            "unmatched ] tags"
        ],
        [
            "unmatched \\] tags"
        ],
        [
            "[b]"
        ],
        [
            "[b attr]invalid attr - no value[/b]"
        ], 
        [
            "[b attr=]invalid attr - no value[/b]"
        ],
        [
            "[[b]invalid tag[/b]"
        ],
        [
            "[b =43]invalid attr[/b]"
        ],
        [
            "[b =]invalid attr[/b]"
        ],
    ];

    public static IEnumerable<object[]> SBBCodeGoodMessages =
    [
        [
            "this is SBBCode message",
            new List<ISBBElement> { new SBBContent("this is SBBCode message") }
        ],

        [
            "[b]bold[/b]",
            new List<ISBBElement>
            {
                new SBBTag("b", [], [
                    new SBBContent("bold")
                ])
            }
        ],

        [
            "[i][b]italic bold[/b][/i]",
            new List<ISBBElement>
            {
                new SBBTag("i", [], [
                    new SBBTag("b", [], [
                        new SBBContent("italic bold")
                    ])
                ])
            }
        ],
        [
            "[style color='red' size=24]this text is red with size 24[/style]",
            new List<ISBBElement>
            {
                new SBBTag("style", [
                    new SBBTagStringAttribute("color", "red"),
                    new SBBTagIntAttribute("size", 24),
                ], [
                    new SBBContent("this text is red with size 24")
                ])
            }
        ],
    ];

    public static IEnumerable<object[]> SBBCodeGoodMessagesWithChecks =
    [
        [
            "this is SBBCode message",
            (IEnumerable<AllowedTag>) [],
            new List<ISBBElement> { new SBBContent("this is SBBCode message") },
        ],

        [
            "[b]bold[/b]",
            (IEnumerable<AllowedTag>)
            [
                new AllowedTag("b", [], false)
            ],
            new List<ISBBElement>
            {
                new SBBTag("b", [], [
                    new SBBContent("bold")
                ])
            }
        ],
        [
            "[style color='red' size=24]this text is red with size 24[/style]",
            (IEnumerable<AllowedTag>)
            [
                new AllowedTag("style", [
                        new AllowedTagAttribute("color", true, new HashSet<Type> { typeof(string) }),
                        new AllowedTagAttribute("size", true, new HashSet<Type> { typeof(int) }),
                    ],
                    false
                )
            ],
            new List<ISBBElement>
            {
                new SBBTag("style", [
                    new SBBTagStringAttribute("color", "red"),
                    new SBBTagIntAttribute("size", 24),
                ], [
                    new SBBContent("this text is red with size 24")
                ])
            }
        ],
        [
            "[style color='red']this text is red with undefined size[/style]",
            (IEnumerable<AllowedTag>)
            [
                new AllowedTag("style", [
                        new AllowedTagAttribute("color", true, new HashSet<Type> { typeof(string) }),
                        new AllowedTagAttribute("size", false, new HashSet<Type> { typeof(int) }),
                    ],
                    false
                )
            ],
            new List<ISBBElement>
            {
                new SBBTag("style", [
                    new SBBTagStringAttribute("color", "red"),
                ], [
                    new SBBContent("this text is red with undefined size")
                ])
            }
        ],
        [
            "[style color='red' extraAttribute=42]this text is red with undefined size[/style]",
            (IEnumerable<AllowedTag>)
            [
                new AllowedTag("style", [
                        new AllowedTagAttribute("color", true, new HashSet<Type> { typeof(string) }),
                    ],
                    true
                )
            ],
            new List<ISBBElement>
            {
                new SBBTag("style", [
                    new SBBTagStringAttribute("color", "red"),
                    new SBBTagIntAttribute("extraAttribute", 42),
                ], [
                    new SBBContent("this text is red with undefined size")
                ])
            }
        ],
    ];
}