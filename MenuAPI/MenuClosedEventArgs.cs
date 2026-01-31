namespace MenuAPI;

public class MenuClosedEventArgs
{
	public bool ClosedByUser { get; set; }

	public bool IsOpeningSubmenu { get; set; }
}
