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
                TurnCubeBlue
            "The prohibited color!" if False:
                DoNothing(rush, 2, six)
            "Not interested, thanks":
                eu "That's enough for today." #[exp:sad, gesture:pray]
                jump another_scene
        #else:
            #jump ignavius

        "Come on, another one!"

    eu "Cool, ah?"

label another_scene:
    return