namespace cutube;

public class TimeHelper
{
    public static int GetStartSeconds(string start)
    {
        var startTime = TimeSpan.ParseExact(start,"hh\\:mm\\:ss",null);
        var startSeconds = (int)startTime.TotalSeconds;
        return startSeconds;
    }
    
    public static int GetEndSeconds(string end)
    {
        var endTime = TimeSpan.ParseExact(end,"hh\\:mm\\:ss",null);
        var endSeconds = (int)endTime.TotalSeconds;
        return endSeconds;
    }
    //TODO:Fazer isso direito.
    public static int GetTimeDiff(string start, string end)
    {
        var startTime = TimeSpan.ParseExact(start,"hh\\:mm\\:ss",null);
        var endTime = TimeSpan.ParseExact(end,"hh\\:mm\\:ss",null);
        
        var startSeconds = (int)startTime.TotalSeconds;
        int endSeconds = (int)endTime.TotalSeconds;
        
        return endSeconds - startSeconds;
    }
}