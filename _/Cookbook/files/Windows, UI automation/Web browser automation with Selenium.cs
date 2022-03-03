/// There are two ways to automate pages in web browser (click links, fill web forms, extract data, etc):
/// - UI element functions (<see cref="elm"/>). See recipe <+recipe>Web automation<>.
/// - <google>Selenium WebDriver</google>. It has more features, for example wait for web page loaded, extract text, execute JavaScript. It can be faster and more reliable. Disadvantages: not so easy to use; always launches new web browser instance.
/// 
/// To use Selenium, install NuGet packages <+nuget>Selenium.Support<> and <+nuget>Selenium.WebDriver.ChromeDriver<>. Then in scripts add code like this:

/*/ nuget -\Selenium.Support; nuget -\Selenium.WebDriver.ChromeDriver; /*/
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;

/// Open web page. It launches new Chrome instance; there is no way to use an existing instance.

var service = ChromeDriverService.CreateDefaultService();
service.HideCommandPromptWindow = true;
using var driver = new ChromeDriver(service);
driver.Navigate().GoToUrl("https://www.quickmacros.com/forum/");

/// Maximize window.

driver.Manage().Window.Maximize();

/// Get page title, URL, HTML source. Extract all text.

print.it(driver.Title);
print.it(driver.Url);
//print.it(driver.PageSource);
var body = driver.FindElement(By.TagName("body"));
print.it(body.Text);

/// Find and click a link. It opens new page and waits util it is loaded.

var e = driver.FindElement(By.LinkText("Resources"));
e.Click();

/// Find a text input field and enter text. To find it use XPath copied from Chrome Developer Tools.

var e2 = driver.FindElement(By.XPath("//*[@id='search']/input[1]"));
e2.SendKeys("find");

/// How to invoke mouse or keyboard actions.

Actions action = new Actions(driver);
action.ContextClick(e2).Perform();

/// Wait. Then the script will close the web browser.

3.s();
dialog.show("Close web browser", x: ^1);

/// More info on the internet.
