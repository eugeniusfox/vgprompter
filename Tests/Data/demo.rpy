define noun = "color"
define b = "Blue"
define adj = "spinning"
define sth  = "cube"
define things =  "[adj] [sth]s"

label start:
    if False: #asdfadf
        "Whatever!"
    eu "Hi there."
    eu "Let me show you the power of [things]!"
    eu "Pick a [noun], would you?" #asdfadf

    while True:
        menu 5000: # [3, 2, 1]:  Introduce a way to set the order of the choices programmatically?
            "Green" if CurrentColorNotGreen:
                TurnCubeGreen
            "[b]":
                TurnCubeBlue()
            "The prohibited color!" if False:
                #SomeOtherDelegate("test")
                pass
            "Not interested, thanks":
                eu "That's enough for today." #[exp:sad, gesture:pray]
                SomeDelegate(1, 2)
                Test(True, False, "s1", 's2', 1.0, 12, 2.0f)
                jump another_scene
        #else:
            #jump ignavus

        "Come on, another one!"

    eu "Cool, ah?"

label another_scene:
    return
