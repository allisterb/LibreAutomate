using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Au;

/// <summary>
/// Performs OCR (text recognition) and find text in OCR results. Contains text to find and OCR parameters.
/// </summary>
/// <remarks>
/// Can be used instead of <see cref="ocr.find"/>.
/// </remarks>
public class ocrFinder {
	readonly object _text;
	readonly int _textType;
	readonly OcrFlags _flags;
	readonly double _scale;
	readonly IOcrEngine _engine;
	readonly int _skip;

	IFArea _area;
	Hash.MD5Result _md5;
	bool _waitNot;

	internal enum Action_ { Find, Wait, WaitNot }

	/// <summary>
	/// Stores text to find and OCR parameters.
	/// </summary>
	/// <exception cref="ArgumentException"><i>text</i> is empty or conatins invalid regular expression.</exception>
	/// <inheritdoc cref="ocr.find(IFArea, string, OcrFlags, double, IOcrEngine, int)" path="/param"/>
	public ocrFinder(string text, OcrFlags flags = 0, double scale = 0, IOcrEngine engine = null, int skip = 0) {
		//**r - regexp (PCRE)
		//**R - Regex (.NET)
		//**i - text, ignore case
		//**t - text
		_textType = text.Starts(false, "**r ", "**R ", "**i ", "**t ");
		if (_textType > 0) text = text[4..];
		if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("empty text");
		_text = _textType switch {
			1 => new regexp(text),
			2 => new Regex(text, RegexOptions.CultureInvariant),
			_ => text
		};

		_flags = flags;
		_scale = scale;
		_engine = engine ?? ocr.engine;
		_skip = skip;
	}

	/// <summary>
	/// Returns an <see cref="ocr"/> object that contains the word index and can click it etc.
	/// </summary>
	public ocr Result { get; internal set; }

	/// <inheritdoc cref="ocr.find(IFArea, string, OcrFlags, double, IOcrEngine, int)"/>
	public ocr Find(IFArea area) => Exists(area) ? Result : null;

	/// <inheritdoc cref="ocr.find(double, IFArea, string, OcrFlags, double, IOcrEngine, int)"/>
	public ocr Find(IFArea area, double waitS) => Exists(area, waitS) ? Result : null;

	/// <returns>If found, sets <see cref="Result"/> and returns true, else false.</returns>
	/// <inheritdoc cref="Find(IFArea)"/>
	public bool Exists(IFArea area) {
		_Before(area, Action_.Find);
		return _Find(false);
	}

	/// <returns>If found, sets <see cref="Result"/> and returns true. Else throws exception or returns false (if <i>waitS</i> negative).</returns>
	/// <inheritdoc cref="Find(IFArea, double)"/>
	public bool Exists(IFArea area, double waitS) {
		bool r = waitS == 0 ? Exists(area) : Wait_(Action_.Wait, waitS < 0 ? waitS : -waitS, area);
		return r || double.IsNegative(waitS) ? r : throw new NotFoundException();
	}

	/// <inheritdoc cref="ocr.wait"/>
	public ocr Wait(double secondsTimeout, IFArea area)
		=> Wait_(Action_.Wait, secondsTimeout, area) ? Result : null;
	//SHOULDDO: suspend waiting while a mouse button is pressed.
	//	Now, eg if finds while scrolling, although MouseMove waits until buttons released, but moves to the old (wrong) place.

	/// <inheritdoc cref="ocr.waitNot"/>
	public bool WaitNot(double secondsTimeout, IFArea area)
		=> Wait_(Action_.WaitNot, secondsTimeout, area);

	internal bool Wait_(Action_ action, double secondsTimeout, IFArea area) {
		if (area.Type == IFArea.AreaType.Bitmap) throw new ArgumentException("Bitmap and wait");
		_Before(area, action);
		_md5 = default;
		try { return wait.forCondition(secondsTimeout, () => _Find(true) ^ _waitNot); }
		finally { _area = null; }
	}

	//called at the start of _Find and Wait_
	void _Before(IFArea area, Action_ action) {
		Not_.Null(area);
		area.Before_(_flags.HasAny(OcrFlags.WindowDC | OcrFlags.PrintWindow));
		_area = area;
		_waitNot = action == Action_.WaitNot;
	}

	/// <summary>
	/// If <b>testing</b> true, the finder after OCR sets <b>result</b>. Then you can access it when text not found (or found).
	/// </summary>
	[ThreadStatic] internal static (bool testing, ocr result) testing_;

	bool _Find(bool waiting) {
		if (!_area.GetOcrData_(_flags, out var b, out var resultOffset)) return false;
		bool inBitmap = _area.Type == IFArea.AreaType.Bitmap;

		if (waiting) { //don't OCR if nothing changed. Can be expensive ($) if using cloud.
			var m = _BitmapHash(b);
			if (m == _md5) {
				if (!inBitmap) b.Dispose();
				return _waitNot;
			}
			_md5 = m;
		}

		var scale = _area.GetOcrScale_(_scale, _engine);
		var a = _engine.Recognize(b, dispose: !inBitmap, scale);
		var r = new ocr(a, _area);
		if (testing_.testing) testing_.result = r;

		if (!_FindText(r)) return false;

		r.AdjustResults_(resultOffset, _flags);

		Result = r;
		return true;

		static unsafe Hash.MD5Result _BitmapHash(Bitmap b) {
			using var d = b.Data(ImageLockMode.ReadOnly);
			return Hash.MD5(new ReadOnlySpan<byte>((void*)d.Scan0, d.Height * d.Stride));
		}
	}

	bool _FindText(ocr r) {
		string s1 = r.TextForFind, s2 = _text as string;
		if (s2 != null) {
			s1 = s1.Replace('I', 'l').Replace('1', 'l').Replace('0', 'O');
			s2 = s2.Replace('I', 'l').Replace('1', 'l').Replace('0', 'O');
		}

		int startIndex = 0, skip = _skip;
		g1:
		int i = -1, i2 = i;
		switch (_text) {
		case regexp rx:
			if (rx.Match(s1, 0, out RXGroup g, startIndex..)) { i = g.Start; i2 = g.End; }
			break;
		case Regex rx:
			if (rx.Match(s1, startIndex) is var m && m.Success) { i = m.Index; i2 = i + m.Length; }
			break;
		default:
			i = s1.Find(s2, startIndex, _textType == 3);
			i2 = i + s2.Length;
			break;
		}

		if (i >= 0) {
			if (skip > 0) { skip--; startIndex = i2; goto g1; }
			r.FoundTextRange = new(i, i2);
			for (int j = r.Words.Length; --j >= 0;) {
				if (r.Words[j].Offset <= i) { r.FoundWordIndex = j; return true; }
			}
		}

		return false;
	}

	///
	public override string ToString() => _text.ToString();
}
