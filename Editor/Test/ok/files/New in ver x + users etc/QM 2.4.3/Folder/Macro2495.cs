out
int w1=win("Untitled - Notepad" "Notepad")
 int w1=win("Dialog1" "#32770")
 SetWindowPos w1 HWND_TOPMOST 0 0 0 0 SWP_NOSIZE|SWP_NOMOVE
SetWindowPos w1 HWND_TOPMOST 0 0 0 0 SWP_NOSIZE|SWP_NOMOVE|SWP_NOACTIVATE
outx GetWinStyle(w1 1)&WS_EX_TOPMOST
1
 SetWindowPos w1 HWND_NOTOPMOST 0 0 0 0 SWP_NOSIZE|SWP_NOMOVE|SWP_NOACTIVATE
SetWindowPos w1 HWND_NOTOPMOST 0 0 0 0 SWP_NOSIZE|SWP_NOMOVE|SWP_NOACTIVATE|SWP_NOOWNERZORDER
outx GetWinStyle(w1 1)&WS_EX_TOPMOST