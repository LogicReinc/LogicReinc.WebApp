# LogicReinc.WebApp
Create cross-platform apps using C# with a HTML/Web UI without the trash dependencies or intrusive development practices.
It aims to provide a simpler, lightweight and more integrated alternative to frameworks such as Electron .NET
### Work in progress

## Motivation
This project started out of the frustration that comes with cross-platform GUI development. And the dislike of various strategies executed in equivalent libraries.

This projects aims to provide a lightweight wrapper that can be implemented on *any* C# browser implementation that provides 2 way communication and simple javascript execution. The core library is completely .Net standard, thus allowing you to integrate this on any platform, including mobile.

Despite it working on such a simple premise, the goal is to allow as close-nit integration with your UI as possible, and by using optional libraries such as LogicReinc.WebApp.Vue remove the need for javascript by 99%, thus allowing you to do all your logic in C#.

## Steps to use
 - Reference LogicReinc.WebApp.dll   (nuget)
 - Reference LogicReinc.WebApp.Mixed (or any other Window implementation)
 - Optionally Reference LogicReinc.WebApp.Vue

Thats it, Use WebWindows.SetManager() once and create your window class and html!

## Nuget
..todo

## Window Implementation
As stated before, the core libraries are completely .net standard and platform independent, thus requiring a seperate library that implements the windows/browser. This is done on purpose to not lock you into a specific setup and allows for easy expansion in the future. 
A lightweight browser is already implemented and ready for use with the LogicReinc.WebApp.Mixed library. This provides several window implementations for Windows/Linux/Mac for Mono using different browsers depending on the operating system (for example, Windows uses Microsofts new WebView library with a  polyfill integrated).

To implement your own window system you simply create a new library of any architecture that references LogicReinc.WebApp and implements IWindowManager and IWebWindow interfaces. And in your client application you pass an instance of the IWindowManager you created to the core library as seen in the example.

## Limitations
Through abstraction a lot of limitations can be alleviated. But the only core limitation is that all communication between C# and javascript will always have some delay for obvious reasons. Thus its best to limit it as much as possible. This IPC delay is completely dependent on the window implementation. But for example the Mixed implementation for Windows has a delay of about 1 to 5ms delay to fetch a javascript value from C#.

## Dependencies
Just Newtonsoft.Json for the core library. 
Window implementation depends on what you use. 
(eg. Mixed just uses Microsft.Toolkit.Forms.UI.Controls.WebView.dll)

## Security
Due to the tight integration with C# it means that depending on your settings you may expose C# functions to your UI, thus be careful what code you trust in your UI.
If you want to limit the exposed functions to javascript set your Window Security to Attributed, this only exposed the methods with the [WebExposed] attribute.

## Professional usage
While this library was started for my own personal projects, I can see the appeal in other projects as well. Be aware that this library achieves a lot of its features through questionable means such as javascript injection. And strange bugs may occur due to that nature. If you're going to use this in anything professional be very aware of the possibility of strange formatting or js dependencies conflicting. Also take extra note to the security of your application and take a look at the underlying logic to make sure there are no exploits.

## What it looks like
On application start:
```C#
static void Main(string[] args)
{
	//Call this once with your chosen window implementation
	WebWindows.SetManager(new MixedWindowManager(os, is64bit)) 
	
	//Now you can create windows anywhere like this.
	var myTestApp = new TestApp();
	myTestApp.Show();'
	...
}
```

Your app window class:
```C#
[AppSize(480,300)] //Optional set size
[ContextResources("TestApp.Web", "")] //Optional embedded resources
[ResourcePage("TestApp.Web.Test.html")] //Optional loaded page on init
[Window(HasBorder = false)]	//Optional settings
[ExposeSecurity(WebExposeType.Attributed)] //Optional Security (Require attributes or not)
public class TestApp : WebWindow
{
	[WebExpose] //Required if security is Attributed
	public void PrintSomething(string msg)
	{
		Console.WriteLine(msg);
	}
	[WebExpose]
	public string GetSomething(string str)
	{
		return "Something:" + str;
	}
}
```
Embedded Resource: TestApp.Web.Test.html
```HTML
<html>
	<head>
	...
	</head>
	<body>
		<div class="bar" onmousedown="StartDragMove()" onmouseup="StopDragMove()">
			..
			<div class="closeButton" onclick="Close()">
				..
			</div>
		</div>
		...
		<script>
			PrintSomething("Started");
			var something = GetSomething("Something");
		</script>
	</body>
</html>
```
With some more styling it could look like this on your desktop:
![example](https://github.com/LogicReinc/LogicReinc.WebApp/raw/master/assets/images/webappdemo.png)

## Vue Integration
By referencing LogicReinc.WebApp.Vue you can utilize Vue exclusively through C#. Removing the need for javascript entirely. You do this by implementing the VueWindow instead.

No javascript required!

![example](https://github.com/LogicReinc/LogicReinc.WebApp/raw/master/assets/images/vueappdemo.png)

```C#
//Settings attributes
..
[AppSize(400, 300)]
[ContextResources("TestApp.Web", "")]
[ResourcePage("TestApp.Web.VueTest.html")]
[ExposeSecurity(WebExposeType.Attributed)]
[VueSettings(RequireExpose = false)]
public class VueTestApp : VueWindow
{
	public override string VueElementID => "#root";
	
	//If VueSettings has RequireExpose enabled
	//You need to add [VueExpose] to public properties
	public int MyCounter { get; set; }
	
	public VueTestApp()
	{
		RegisterComponent(typeof(Counter))
	}
	
	[WebExpose]
	public void Increment()
	{
		MyCounter++;
		//If variables are changed outside of vue/js called methods
		//You may have to call NextTick();
	}

	[VueTemplate("TestApp.Web.VueTestComp.html")]
	public class Counter : VueComponent
	{
		public int Count { get; set; }
		
		public Counter(string id, VueWindow parent) : base(id, parent){}
		
		public void Increment()
		{
			Count++;
		}
		public override void Mounted() 
		{
			Console.Writeline("Component mounted!");
		}
	}
}
```
TestApp.Web.VueTest.html
```Html
<html>
<head>
    ...
</head>
<body>
    <div id="root" style="text-align: center; padding-top: 20px;">
        <h2>Vue Testing</h2>
        <h3>{{MyCounter}}</h3>
        <md-button class="md-raised md-primary" @click="Increment()">
            Increase
        </md-button>
        <counter-comp></counter-comp>
    </div>

    <script src="scripts/vue.js"></script>
    <script src="scripts/vue-material.min.js"></script>
    <script>
        Vue.use(VueMaterial.default);
    </script>
</body>
</html>
```
TestApp.Web.VueTestComp.html
```html
<div>
	<h4>Counter: {{Count}}</h4>
	<md-button class="md-raised md-accent" @click="Increment()">
		Increment
	</md-button>
</div>
```