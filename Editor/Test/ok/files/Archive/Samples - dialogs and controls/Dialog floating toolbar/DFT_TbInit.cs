 /DFT_Main
function hDlg tbId firstButtonId $textList $iconsList

 Initializes toolbar control.

int htb=id(tbId hDlg)

 ---------------------------
int nbuttons=numlines(iconsList)
int i; ARRAY(str) aa

SetWinStyle htb TBSTYLE_FLAT|TBSTYLE_TOOLTIPS|CCS_NODIVIDER 1

 create imagelist
int il=ImageList_Create(16 16 ILC_MASK|ILC_COLOR32 0 nbuttons)
aa=iconsList
for(i 0 nbuttons) ImageList_ReplaceIcon(il -1 GetFileIcon(aa[i]))
SendMessage htb TB_SETIMAGELIST 0 il

 create TBBUTTON array
ARRAY(TBBUTTON) a.create(nbuttons)
aa=textList
for i 0 a.len
	TBBUTTON& t=a[i]
	t.idCommand=firstButtonId+i
	t.iBitmap=i
	t.iString=SendMessage(htb TB_ADDSTRING 0 aa[i]) ;;note: the string must be terminated with two 0. str variables normally have two 0, but if you will use unicode...
	t.fsState=TBSTATE_ENABLED

SendMessage(htb TB_BUTTONSTRUCTSIZE sizeof(TBBUTTON) 0)
SendMessage(htb TB_ADDBUTTONS a.len &a[0])
SendMessage(htb TB_AUTOSIZE 0 0)