int useIE=1

str s sh sr
s.getfile("$desktop$\test.txt")
 s.getfile("$qm$\winapiqm.txt")
s-"--7d23542a1a12c2[]Content-Disposition: form-data; name=''userfile''; filename=''test.txt''[]Content-Type: text/plain[][]"
s+"[]--7d23542a1a12c2--[]"
sh="Content-Type: multipart/form-data; boundary=7d23542a1a12c2[]"

if(!useIE)
	IntPost "http://www.nightchatter.com/uploader.php" s sr sh
else
	VARIANT v vh
	vh=sh
	ARRAY(byte) a.create(s.len+1)
	memcpy(&a[0] s a.len)
	v.attach(a)
	
	int h=win
	SHDocVw.InternetExplorer b._create
	act h
	b.Navigate("http://www.nightchatter.com/uploader.php" @ @ v vh)
	 b.Navigate("http://www.google.com")
	1
	rep() 0.1; if(!b.Busy) break
	MSHTML.IHTMLDocument2 doc=b.Document
	sr=doc.body.innerText

out sr