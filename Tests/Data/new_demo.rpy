label start:
    if Something.Else() && true:
        eu "Something!"
    ge "Else!"
    cs:
        var x = new List<int>();
        x.Add(0);
        x.Add(1);
        foreach (var e in x) {
            Console.WriteLine(e);
        }

label end:
    $ SomeAction();
    SomeAction
    menu:
        eu "First choice" if true:
            "Test"
        "Second choice" if 0 == 0:
            "Test"
    pass