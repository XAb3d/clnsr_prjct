namespace CleanserBlazorUI.Interface;
public class EventService
{
    // Define an event that returns data via Task<List<string>>
    public event Func<Task<List<string>>> OnFunctionCalled;

    public async Task<List<string>> CallFunctionAsync()
    {
        if (OnFunctionCalled != null)
            return await OnFunctionCalled.Invoke(); // Return the data
        else
            return new List<string>(); // Return default if no subscribers
    }
}