namespace Gtacnr.Client.HUD;

public class NuiDialog
{
	public string Header { get; set; }

	public string Content { get; set; }

	public string InputType { get; set; } = "text";

	public string Placeholder { get; set; }

	public string DefaultText { get; set; }

	public string SubmitLabel { get; set; } = "Submit";

	public string CancelLabel { get; set; } = "Cancel";

	public int MaxLength { get; set; } = -1;

	public NuiDialog(string header, string content, string placeholder = "")
	{
		Header = header;
		Content = content;
		Placeholder = placeholder;
	}
}
