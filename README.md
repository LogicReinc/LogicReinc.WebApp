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

## Platforms
Supported platforms are completley dependent on your WindowManager implementation. In theory you can implement this framework on any browser with 2 way communication.
Currently implemented in Mixed WindowManager:

 - Windows 32bit : Microsoft Webview
 - Windows 64bit : Microsft Webview / Chromium
 - Linux 64bit : Chromium
 - Android : Android Webview (likely chromium)
	
This list should expand quick, expect Android, Mac and other architectures such as ARM soon.

## Window Implementation
As stated before, the core libraries are completely .net standard and platform independent, thus requiring a seperate library that implements the windows/browser. This is done on purpose to not lock you into a specific setup and allows for easy expansion in the future. 
A lightweight browser is already implemented and ready for use with the LogicReinc.WebApp.Mixed library. This provides several window implementations for Windows/Linux/Mac for Mono using different browsers depending on the operating system (for example, Windows uses Microsofts new WebView library with a  polyfill integrated).

To implement your own window system you simply create a new library of any architecture that references LogicReinc.WebApp and implements IWindowManager and IWebWindow interfaces. And in your client application you pass an instance of the IWindowManager you created to the core library as seen in the example.
To enable the IPC to work you will have to inject a javascript function like this:
```javascript
	function _IPC_send(str){
		some.method.to.host(str);
	}
```
And then pass the str value to the OnIPC event in the implemented interface.


## Limitations
Through abstraction a lot of limitations can be alleviated. But the only core limitation is that all communication between C# and javascript will always have some delay for obvious reasons. Thus its best to limit it as much as possible. This IPC delay is completely dependent on the window implementation. But for example the Mixed implementation for Windows has a delay of about 1 to 5ms delay to fetch a javascript value from C#.

## Dependencies
Just Newtonsoft.Json for the core library. 

Other dependencies depend on your WindowManager:
 - Mixed (Force Chromium)
	- CefGlue
 - Mixed (non-Chromium)
	- CefGlue (for Linux)
	- Microsoft WebView (for Windows)
 - Android
	(nothing, works using default Xamarin Android App)

## Security
Due to the tight integration with C# it means that depending on your settings you may expose C# functions to your UI, thus be careful what code you trust in your UI.
If you want to limit the exposed functions to javascript set your Window Security to Attributed, this only exposed the methods with the [WebExposed] attribute.

## Professional usage
This library is still in prototype phases and is not recommended for profesional use. Use at own risk.

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
	..
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
![example](https://github.com/LogicReinc/LogicReinc.WebApp/raw/master/assets/images/webappdemo.PNG)

## Javascript Integration
Javascript
```javascript
var testStructure = {
	subFunction(){
		Print("whatever");     
	},
	intVal : 123
};
function testFunction(str){
	Print(str);  
}
DoSomething();
```
C#
```C#
public void DoSomething()
{
	JS.testFunction("someParameter");
	JS.testStructure.subFunction();
    JS.SomeValue = "Testing?";
    JS.SomeObj = new Object();
    JS.SomeObj.Str = "Whatever";
	
	string someValue = JS.SomeValue;
	string someObjStr = JS.SomeObj.Str;
	int testStrIntVal = JS.testStructure.intVal;
	int windowWidth = JS.window.outerWidth;
}
```


## Vue Integration
By referencing LogicReinc.WebApp.Vue you can utilize Vue exclusively through C#. Removing the need for javascript entirely. You do this by implementing the VueWindow instead.

No javascript required!

![example](https://github.com/LogicReinc/LogicReinc.WebApp/raw/master/assets/images/vueappdemo.PNG)

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
        <counter></counter>
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


## LogicReinc.WebApp.Android (Work-in-progress)
Due to Android being fundamentally different than desktop windows, you require some different steps, but usage is nearly identical and you can still share classes with other operating systems if you like. The only difference would be the entrypoint.

To use your WebWindow (or VueWindow) implementation, instead of using new WebWindow().Show(), you use 
```C#
	StartActivity(typeof(WebAppActivity<WebWindow>))
```
Besides that, there is no difference in usage. Some methods such as SetSize/Position etc will not do anything because they arent relevant and thus will always use full screen.

You will also have to add a reference to Mono.Android.Export in your end-project.