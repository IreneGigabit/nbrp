<PRE><CODE><html>
   <head>
	<title>Upload a File With ASP!</title>
   </head>
   <body>
	<form action="filePost.asp" enctype="multipart/form-data" method="post" name="f" id="f">
	   Pick a file to upload:<br>
	   <input type="file" name="test" size="50">
	   <br >
	   <input type="radio" name="filename" value="original" checked>
		Use filename from disk
	   <br >
	   <input type="radio" name="filename" value="userSpecified">
		Use the following name:
	   <input type="text" name="userSpecifiedName" value="">
	   <br >
	   <input type="submit" value="submit">
	</form>
   </body>
</html>
</CODE></PRE>