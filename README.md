# Code refactorings for VisualStudio

- [Invert assignment direction](#invert-assignment-direction)
- [Generate mapping](#generate-mapping)
- [Rename variable after type/expression](#rename-variable-after-typeexpression)
- [Introduce parameter object](#introduce-parameter-object)
- [MediatR add RequestHandler and Request](#mediatr-add-requesthandler-and-request)
- [Convert to full WPF property](#convert-to-full-wpf-property)
- [Encapsulate field for WPF](#encapsulate-field-wpf)
- [Copy paste with conversion](#copypaste-with-case-conversion)


## Invert assignment direction

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.InvertAssignmentDirection?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.InvertAssignmentDirection)

<img src="Documentation/InvertAssignmentDirection.sampleusecase.gif" width="678">

Visual Studio code refactoring that allows swapping arguments around the equal sign in an assignment statement. It works on single or many selected assignment statements at once.


## Generate mapping

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.GenerateMapping?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.GenerateMapping)

<img src="Documentation/GenerateMapping.sampleusecase.gif" width="710">

Visual Studio code refactoring that generates mapping code, between method input parameters and method return type. If any of them is void, "this" is used instead. Design time AutoMapper alternative.

What can be generated:

- mapping method
- copy constructor
- memento method get state
- memento method set state
- deconstruct method

<img src="Documentation/GenerateMapping.whatcanbegenerated.gif" width="710">

General rules:

- mapping is available only for methods/constructors with an empty body
- mapping can be done for more than one input parameter
- for the left side of the assignment considered are all writable fields and properties
- for the right side of the assignment considered are all readable fields and properties
- for the assignment of mutable reference type, a "new" instance is always created 
- recursive mapping is not, and will not be supported
- mapping to tuples is supported



## Rename variable after type/expression 

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.RenameVariableAfterType?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.RenameVariableAfterType)

<img src="Documentation/RenameVariableAfterType.sampleusecase.gif" width="710">


Visual Studio code refactoring that changes a variable name, to an auto-generated name from the variable type or expression used in the assignment, e.g. "Foo x =" => "Foo foo ="

- a simple heuristic, not AI based (yet)
- works on a single or many variables at once
- works on a single method parameter




## Introduce parameter object

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.IntroduceParameterObject?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.IntroduceParameterObject)

<img src="Documentation/IntroduceParameterObject.sampleusecase.gif" width="710">

Visual Studio implementation of code refactoring [Introduce Parameter Object](https://refactoring.com/catalog/introduceParameterObject.html) from Martin Fowler's book "Refactoring, Improving the Design of Existing Code".



##### Limitations 
- it does not support generic type parameters
- updating method body is done syntactically, not semantically, it means they may appear conflicts if a method parameter shares the same name with something else



## MediatR add RequestHandler and Request

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.MediatRAddRequestHandlerAndRequest?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.MediatRAddRequestHandlerAndRequest)

<img src="Documentation/MediatRAddRequest.sampleusecase.gif" width="800">

<img src="Documentation/MediatRAddRequestHandler.sampleusecase.gif" width="800">

Visual Studio code refactoring that adds Command class which implements [MediatR](https://github.com/jbogard/MediatR) IRequest<> interface and handler class which implements [MediatR](https://github.com/jbogard/MediatR) IRequestHandler<> interface.
It only creates empty boilerplate code required by MediatR based on the selected method name and return type.


##### Limitations 
- does not support generic type parameters

##### Changelog 

version 1.5
- RequestHandler can also be generated from an existing Request class



## Convert to full WPF Property

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.ConvertToFullWPFProperty?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.ConvertToFullWPFProperty)

<img src="Documentation/ConvertToFullWPFProperty.sampleusecase.gif" width="699">

Visual Studio code refactoring that replaces an auto-property with full property implementation that consists invocation of OnPropertyChanged in a setter. It can convert many auto-properties at once.



## Encapsulate field (WPF)

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.EncapsulateFieldForWPF?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.EncapsulateFieldForWPF)

<img src="Documentation/EncapsulateFieldForWPF.sampleusecase.gif" width="678">

Visual Studio code refactoring that creates full property implementation with an invocation of OnPropertyChanged in a setter for a selected set of backing fields.



## Copy/Paste with case conversion

[![download](https://img.shields.io/visual-studio-marketplace/v/NeVeS.CopyPasteWithConversion?label=download&style=for-the-badge)](https://marketplace.visualstudio.com/items?itemName=NeVeS.CopyPasteWithConversion)

<img src="Documentation/CopyPasteWithConversion.sampleusecase.gif" width="710">

Visual Studio extension that adds two commands to the text editor context menu that do case conversion before copy/paste. 

Two kinds of conversion are available:
- Copy as sentence case (also splits selected text to separate words)
- PasteAsPascalCase
