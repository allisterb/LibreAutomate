 /
function# hwnd priority [flags] ;;flags: 1 hwnd is tid

int tid
if(flags&1) tid=hwnd
else tid=GetWindowThreadProcessId(hwnd 0)

__Handle ht=OpenThread(THREAD_SET_INFORMATION 0 tid)

ret SetThreadPriority(ht priority)