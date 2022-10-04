namespace Au.Types;

/// <summary>
/// <see cref="script.role"/>.
/// </summary>
public enum SRole {
	/// <summary>
	/// The task runs as normal .exe program.
	/// It can be started from editor or not. It can run on computers where editor not installed.
	/// </summary>
	ExeProgram,

	/// <summary>
	/// The task runs in Au.Task.exe process, started from editor.
	/// </summary>
	MiniProgram,

	/// <summary>
	/// The task runs in editor process.
	/// </summary>
	EditorExtension,
}

/// <summary>
/// Flags for <see cref="script.setup"/> parameter <i>exception</i>. Defines what to do on unhandled exception.
/// Default is <b>Print</b>, even if <b>script.setup</b> not called (with default compiler only).
/// </summary>
[Flags]
public enum UExcept {
	/// <summary>
	/// Display exception info in output.
	/// </summary>
	Print = 1,

	/// <summary>
	/// Show dialog with exception info.
	/// </summary>
	Dialog = 2,
}

/// <summary>
/// The default compiler adds this attribute to the main assembly if role is miniProgram or exeProgram.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PathInWorkspaceAttribute : Attribute {
	/// <summary>Path of main file in workspace.</summary>
	public readonly string Path;

	///
	public PathInWorkspaceAttribute(string path) { Path = path; }
}

/// <summary>
/// The default compiler adds this attribute to the main assembly if using non-default references (meta r or nuget). Allows to find them at run time. Only if role miniProgram (default) or editorExtension.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class RefPathsAttribute : Attribute {
	/// <summary>Dll paths separated with |.</summary>
	public readonly string Paths;

	/// <param name="paths">Dll paths separated with |.</param>
	public RefPathsAttribute(string paths) { Paths = paths; }
}

/// <summary>
/// The default compiler adds this attribute to the main assembly if using nuget packages with native dlls. Allows to find the dlls at run time. Only if role miniProgram (default) or editorExtension.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NativePathsAttribute : Attribute {
	/// <summary>Dll paths separated with |.</summary>
	public readonly string Paths;

	/// <param name="paths">Dll paths separated with |.</param>
	public NativePathsAttribute(string paths) { Paths = paths; }
}
