# Wenku8Downloader

1.
Form2.cs line:169 Change Encoding.GetEncoding("gbk") to encoding == "gbk" ? Encoding.GetEncoding("gbk") : Encoding.UTF8 .

2.
Add cookie function (save in a txt file, including username, password, default save path).

Create a button to show a dialog(new form) to edit cookie.
Read cookie.txt when load form1 and form3(edit dialog).
Save file when clicking "confirm" button on form3.

3.
Set Selenium default download path in Form2.cs 

line:23 Change private "static" IWebDriver driver to private IWebDriver driver

line:33 Change private "static" void initSelenium() to private void initSelenium()

line:45 Add 
if(!string.IsNullOrWhiteSpace(folder))
  options.AddUserProfilePreference("download.default_directory", folder);
  
line:46 Change "driver" = new ...; to "_driver" = new ...;

line:50 Change private "static" void initSelenium() to private void initSelenium()

line:53 Change try { "driver".Close(); } to try { "_driver".Close(); }

line:55 Change try { "driver".Quit(); } to  try { "_driver".Quit(); }

line:56 Change try { "driver".Dispose(); } to try { "_driver".Dispose(); }
