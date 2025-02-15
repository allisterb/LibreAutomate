/// For keyboard, autotext, mouse and window triggers you can specify options. Either in trigger arguments or like <mono>Triggers.Options...<> . Options specified like <mono>Triggers.Options...<> are applied to triggers added afterwards.

///This is a full working script. To test it, create new script and paste all code. Or in file "Keyboard triggers" paste code from #region. Then click the Run button.

using Au.Triggers;
var Triggers = new ActionTriggers();
var hk = Triggers.Hotkey;

#region you can paste this code in file "Keyboard triggers"

//these triggers have default options
hk["Alt+1"] = o => { print.it(o); };
hk["Alt+2"] = o => { print.it(o); };

//these triggers... TODO
//Triggers.Options.
hk["Alt+3"] = o => { print.it(o); };
hk["Alt+4"] = o => { print.it(o); };

#endregion

Triggers.Run();
