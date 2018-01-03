define noun = "color"
define b = "Blue"
define adj = "spinning"
define sth  = "cube"
define things =  "[adj] [sth]s"

label start (location='home'):
	# """First label"""

    if False: #asdfadf
        "Whatever!"
    eu (mood='happy') "Hi there."
    eu "Let me show you the power of [things]!"
    eu "Pick a [noun], would you?" #asdfadf

    while True:
        menu (id='first_menu', timeout=5000):
            (default=True) "Green" if CurrentColorNotGreen:
                TurnCubeGreen
            "[b]":
                TurnCubeBlue()
            "The prohibited color!" if False:
                #SomeOtherDelegate("test")
                pass
            "Not interested, thanks":
                eu (mood='sad') "That's enough for today."
                SomeDelegate(1, 2)
                Test(True, False, "s1", 's2', 1.0, 12, 2.0f)
                jump another_scene
        #else:
            #jump ignavus

        "Come on, another one!"

    eu "Cool, ah?"

label another_scene:
    return
