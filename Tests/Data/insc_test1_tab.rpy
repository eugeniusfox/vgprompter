define xyz = "XYZXYZXYZ"
define abc = "ABCABCABC"

label start:

	"First [abc] anonymous line [xyz]."
	char1 "Second line."
	char1 "Third line."  # Test quoted

	menu:  # Test unquoted
		"First anonymous choice [xyz]":
			char1 "First choice line."
		tag "Second named choice":
			char1 "Second choice line."
		"Third conditional anonymous choice" if False:
			char1 "Third choice line."
		tag "Fourth conditional named choice" if True:
			char1 "Fourth choice line."

	if True:
		char1 "Is true."
	else:
		char1 "Is false."

	while True:
		char1 "While line."

		menu:
			"asd":
				call first
			"sdf":
				jump first

label first:
	char "asfasdf"
	return