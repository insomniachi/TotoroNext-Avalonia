using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class Initializer : IInitializer
{
    public void Initialize()
    {
        TsukiHimeLocalData.LoadData();
    }
}