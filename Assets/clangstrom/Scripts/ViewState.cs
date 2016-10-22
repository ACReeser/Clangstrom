
public enum ViewState { Visual = 0, Thermal, Echolocation, Gamma }

public static class ViewStateExtensions
{
    public static ViewState Next(this ViewState v)
    {
        var next = v + 1;
        if (next > ViewState.Echolocation)
            next = 0;
        return next; 
    }
}
