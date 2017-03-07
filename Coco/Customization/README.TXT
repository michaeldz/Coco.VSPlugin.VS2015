Description:		This file contains information about necessary modifications of the Coco/R source code.
Author:				Thomas Reinthaler

This project contains the source code of Coco/R (available at www.ssw.jku.at) with some modifications that were necessary to integrate Coco/R into Microsoft Visual Studio 2010.

----------------------------------------------------------------------
I. List of modifications to orignal source
----------------------------------------------------------------------

-----------------------------------
i) Integrate mappings between semantic actions in Coco/R Grammar file and generated source files (Scanner.cs, Parser.cs)
---
In order to integrate this mappings, the files "Mapping.cs", "BufferHelper.cs" and "Writer.cs" must be added to the orignal source. 
Afterwards, the file ParserGen.cs has to be modified at the lines which are followed by a comment starting with "Coco-Plugin:". The changes are quite self-explaining which makes it no problem to adapt the source.


-----------------------------------
ii) Enable code completion for the Coco/R Grammar (not embedded C#-code!)
---
To make code completion working, the existing "Parser.cs" file has to be marked as partial, the file "Customization/Parser.cs" has to be added to the orignal source.
A private flag "generateSource" is added in the custumization-part, which needs to be handled inside the original "Parser.cs". If this flag is false, no code file is generated (because it's unnecessary for our purpose). Again, this change is laballed with a comment starting with "Coco-Plugin:".
The partial method "Notify" with the signature "partial void Notify(int position);" has to be added to the original "Parser.cs".
The only thing which is still missing is that the original Parser has to call this partial method at certain points, which can be found in "GrammerNotificationPoints.htm".
The parser has to call this method with the corresponding number as param.


----------------------------------------------------------------------
II. Explanation of code completion
----------------------------------------------------------------------

-----------------------------------
i) Introduction
---
Code completion for Coco/R Grammar tokens is done by parsing the actual source code by the original Coco/R parser.
The customized parser looks for the requested code-completion point. The orignal parser notifies the customized parser about the current context. When the current token ("t"-member) is before the requested completion-point and the look-ahead token ("la"-member) is after the requested completion-point, the parser found the point in the Grammar file where code-completion should be applied. This completion-point is only used in case no error occured in the Coco/R grammar file so far.
Some possible completion-points give additional information (brace was opened, token was defined, production was defined). This information is stored by the customized parser as it is used to improve code-completion (e.g. the last opened brace is suggested as possible completion, declared tokens are suggested in other tokens or productions, ...).
The customized parser handles encountered charsets, tokens, productions by itself and DOES NOT rely on the orignal parser as some tokens are added internally by the orignal parser but should not be used for code-completion (e.g. strings in productions are stored as tokens, eof-token is always added). Another reason for collecting this information by the customized parser is that we want the code-completion part to be as independent from the original parser as possible.

After the file was parsed, the customized parser determines tokens, which are valid at the encountered completion-point and returns them as an enumeration of CocoTokens. This information can be handled within the Plugin-part to display according completions.

-----------------------------------
ii) How to use code completion
---
To use this code completion, you first have to create a parser as used (normally, the scanner is provided with a buffer based on a memory-stream).
Afterwards, you have to call the "InitateCodeCompletion"-method. Which sets up partial parsing (disables code-generation, initializes internal structures). Now you can call the "GetPossibleTokens"-method to get the possible tokens for the requested position. Note that the parameters of this function are one-based (means the first line has number 1, same for column param).