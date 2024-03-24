namespace WebUI.Types.Internal;

public interface ITryParseable<in TIn, TOut>
{
    public static abstract bool TryParse(TIn? value, out TOut? @out);
}
