# Wenku8Downloader

1.
Form2.cs line:169 
Change Encoding.GetEncoding("gbk") to 
encoding == "gbk" ? Encoding.GetEncoding("gbk") : Encoding.UTF8 

2.
add cookie function (save in a txt file, including username, password, default save path)
create a button to show a dialog(new form) to edit cookie
read cookie.txt when load form1 and form3(edit dialog)
save file when clicking "confirm" button on form3
