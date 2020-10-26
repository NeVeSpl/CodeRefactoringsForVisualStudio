
1. [Invert assignment direction](#InvertAssignmentDirection)
2. [Convert to full WPF property](#ConvertToFullWPFProperty)
3. [Encapsulate field for WPF](#EncapsulateFieldForWPF)
4. [Introduce parameter object](#IntroduceParameterObject)


#### <a name="InvertAssignmentDirection"></a>1. Invert assignment direction - [download](https://marketplace.visualstudio.com/items?itemName=NeVeS.InvertAssignmentDirection) 

<img src="Documentation/InvertAssignmentDirection.sampleusecase.gif" width="678">

Visual Studio code refactoring that allows swapping arguments around the equal sign in an assignment statement. It works on single or many selected assignment statements at once.

##### Changelog 

version 1.5
- icon added to vsix

version 1.3

- assignment expression that consists of one or two indexers can also be inverted
``
arr[i] = foo[j]; //-> foo[j] = arr[i];
``


#### <a name="ConvertToFullWPFProperty"></a>2. Convert to full WPF Property - [download](https://marketplace.visualstudio.com/items?itemName=NeVeS.ConvertToFullWPFProperty)

<img src="Documentation/ConvertToFullWPFProperty.sampleusecase.gif" width="699">

Visual Studio code refactoring that replaces an auto-property with full property implementation that consists invocation of OnPropertyChanged in a setter.
It can convert many auto-properties at once.

##### Changelog

version 1.5
- icon added to vsix

version 1.4

- performance improvement, a new way of determining the name of a method that usually is called "OnPropertyChanged "
- a more robust way of determining what prefix for a backing field should be used



#### <a name="EncapsulateFieldForWPF"></a>3. Encapsulate field (WPF) - [download](https://marketplace.visualstudio.com/items?itemName=NeVeS.EncapsulateFieldForWPF)

<img src="Documentation/EncapsulateFieldForWPF.sampleusecase.gif" width="678">

Visual Studio code refactoring that creates full property implementation with an invocation of OnPropertyChanged in a setter for a selected set of backing fields.

##### Changelog

version 1.5
- icon added to vsix

version 1.4
- performance improvement, a new way of determining the name of a method that by convention is called "OnPropertyChanged "


#### <a name="IntroduceParameterObject"></a>4. Introduce parameter object - [download](https://marketplace.visualstudio.com/items?itemName=NeVeS.IntroduceParameterObject)

<img src="Documentation/IntroduceParameterObject.sampleusecase.gif" width="710">

Visual Studio implementation of code refactoring [Introduce Parameter Object](https://refactoring.com/catalog/introduceParameterObject.html) from Martin Fowler's book "Refactoring, Improving the Design of Existing Code".



##### Limitations 
- it does not support generic type parameters

##### Changelog 

version 1.1
- fixed crash caused by array parameter