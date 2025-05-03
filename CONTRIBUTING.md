# Contributing to CMDInjector

Welcome, once again;)

("once again": if you recognize me somehow. For example with WhatsWin project)

This is a HUGE application ahead. While everything is sharping (or not depending at the time you read this), please still follow my documentation.

This document explains about piece of codes, navigation routes (and URI?), and how CMDInjector works.

## Requirements

* C# 7.3 knowledge
* Git? A must. It is also needed that you know how to use Git the command (yes run from your command prompt), although it is not strictly required from this project.
* Visual Studio 2017 with UWP workload.
* A Windows 10 Mobile device with build >= 10240 installed. Remember: MOBILE. Emulator may work, but they do not save the injection for further tests.

## Project structure

Very self-explantory: both the title and the project content.

So I will let you figure it out.

## Targets

* Make the code as clean and self-explantory as possible.
* Make the code `#region`s-based
* Reduce the amount of duplicate code, `if-else if-else`s
* Type less, do more, and faster too

## Extensions

They are placed in `CMDInjectorHelper\Extension.cs`.

Supported types:

* Integers
* Bool
* String
* Windows.UI.Xaml.{Visibility,FrameworkElement}
* Windows.UI.Xaml.Application
* Windows.UI.Xaml.Controls.{Panel}

And much more, growing along with its needs.

They are used a LOT in the application. For example:

This checks the currently injected version:

```c#
Globals.InjectedBatchVersion.ToInt32() < Globals.currentBatchVersion
//                           ^^^^^^^^^ An extension method!
```

To quickly show or hide a UI element:

```c#
Element.Visible();
// or
Element.Collapse();
```

Instead of:

```c#
Element.Visible();
Element.Collapse();
```

You also can do this:

```c#
Element.Visibility = AppSettings.CoolFeature.ToVisibility();
//                               ^^^^^^^^^^^ a boolean
```

Some methods allow you to chain themself:

```c#
Element
	.Visible(AppSettings.CoolFeature)
	.AddChildren(aLabel)
	.AddChildren(aComboBox)
	.AddChildren(
		aPanel
			.AddChildren(stuff)
			.AddChildren(confirmationButton)
	);
```

One thing which is widely used is file-checking. Normally we will place our stuff in `\Windows\System32`, no?

Here comes our beloved's:

```c#
"CMDInjectorVersion.dat".IsAFileInSystem32() // checks for file existence in \Windows\System32
"CMDInjector".IsADirInSystem32() // checks for directory existence in \Windows\System32
"Contents\\Scripts\\Test.bat".IsAFileIn(Helper.InstalledLocation.Path)
```

Also you can quickly read a file:

```c#
"CMDInjectorVersion.dat".ReadFromDir("C:\\Windows\\System32");
```

Oh, there is also a function that can check if a string variable is NOT empty:

```c#
test.HasContent()
```

Is the same as:

```c#
!string.IsNullOrWhiteSpace(test)
```

## Application routes

These are used for Start tiles.

* Application update: `Updation` and `DownloadUpdate`