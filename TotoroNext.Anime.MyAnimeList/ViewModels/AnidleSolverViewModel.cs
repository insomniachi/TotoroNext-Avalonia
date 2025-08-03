using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.MyAnimeList.ViewModels;

[UsedImplicitly]
public partial class AnidleSolverViewModel(IFactory<IMetadataService, Guid> metadataFactory) : ObservableObject, IAsyncInitializable
{
    private readonly IMetadataService _metadataService = metadataFactory.Create(Module.Id);

    [ObservableProperty] public partial string Query { get; set; } = "";
    [ObservableProperty] public partial AnimeModel? SelectedSuggestion { get; set; }
    [ObservableProperty] public partial string Answer { get; set; } = "";
    [ObservableProperty] public partial DateTime? Date { get; set; } = new DateTime(2025, 7, 13);
    [ObservableProperty] public partial List<AnimeModel> PossibleAnswers { get; set; } = [];
    public ObservableCollection<AnimeModel> Suggestions { get; } = [];
    public HashSet<string> CorrectGenres { get; } = [];
    public HashSet<string> IncorrectGenres { get; } = [];
    public int? MinimumYear { get; set; }
    public int? MaximumYear { get; set; }
    public float? MinimumScore { get; set; }
    public float? MaximumScore { get; set; }

    public int Round { get; set; } = 1;

    public async Task InitializeAsync()
    {
        _ = await _metadataService.GetGenresAsync();

        this.WhenAnyValue(x => x.Query)
            .Where(x => x is { Length: > 2 })
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(_metadataService.SearchAnimeAsync)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(list =>
            {
                Suggestions.Clear();
                Suggestions.AddRange(list);
            });

        this.WhenAnyValue(x => x.SelectedSuggestion)
            .WhereNotNull()
            .Subscribe(suggestion =>
            {
                Query = "";
                Answer = suggestion.Title;
            });

        this.WhenAnyValue(x => x.Answer)
            .Where(x => !string.IsNullOrEmpty(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(TryAnswer)
            .Subscribe();
    }

    [RelayCommand]
    private void AttemptGuess(AnimeModel anime)
    {
        Answer = anime.Title;
    }

    private async Task<Unit> TryAnswer(string answer)
    {
        if (Date is null || string.IsNullOrEmpty(answer))
        {
            return Unit.Default;
        }

        var response = await "https://cms.aniguessr.com/wp-json/aniguessr/v1/animdle"
                             .AppendQueryParam("answer", answer)
                             .AppendQueryParam("round", Round)
                             .AppendQueryParam("date", Date.Value.ToString("yyyyMMdd"))
                             .GetJsonAsync<AnidleAnswerResponse>();

        if (response.IsCorrect)
        {
            await MessageBox.ShowAsync("Correct", "", MessageBoxIcon.Success);
            return Unit.Default;
        }

        AnidleCriteria<string>[] genresAndThemes = [..response.Data.Genre, ..response.Data.Theme];
        foreach (var item in genresAndThemes)
        {
            if (item.IsValid)
            {
                CorrectGenres.Add(item.Value);
            }
            else
            {
                IncorrectGenres.Add(item.Value);
            }
        }

        if (response.Data.Year.IsValid)
        {
            MinimumYear = MaximumYear = response.Data.Year.Value;
        }
        else
        {
            if (response.Data.Year.Compare == ">")
            {
                MaximumYear = int.Min(MaximumYear ?? int.MaxValue, response.Data.Year.Value - 1);
            }
            else
            {
                MinimumYear = int.Max(MinimumYear ?? int.MinValue, response.Data.Year.Value + 1);
            }
        }

        var score = float.Parse(response.Data.Score.Value);
        if (response.Data.Score.IsValid)
        {
            MinimumScore = MaximumScore = score;
        }
        else
        {
            if (response.Data.Score.Compare == ">")
            {
                MaximumScore = float.Min(MaximumScore ?? float.MaxValue, score);
            }
            else
            {
                MinimumScore = float.Max(MinimumScore ?? float.MinValue, score);
            }
        }

        try
        {
            PossibleAnswers = await _metadataService.SearchAnimeAsync(new AdvancedSearchRequest
            {
                IncludedGenres = [..CorrectGenres],
                ExcludedGenres = [..IncorrectGenres],
                MaximumScore = MaximumScore is null ? null : (float)Math.Round(MaximumScore.Value, 2),
                MinimumScore = MinimumScore is null ? null : (float)Math.Round(MinimumScore.Value, 2),
                MinYear = MinimumYear,
                MaxYear = MaximumYear
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Round++;

        return Unit.Default;
    }
}

public class AnidleCriteria<T>
{
    [JsonPropertyName("valid")] public bool IsValid { get; set; }
    [JsonPropertyName("value")] public T Value { get; set; } = default!;
    [JsonPropertyName("compare")] public string Compare { get; set; } = "";
}

[UsedImplicitly]
public class AnidleAnswerResponse
{
    [JsonPropertyName("response")] public bool IsCorrect { get; set; }
    [JsonPropertyName("data")] public AnidleAnswerResponseData Data { get; set; } = new();
}

public class AnidleAnswerResponseData
{
    [JsonPropertyName("response")] public AnidleCriteria<string> Title { get; set; } = new();
    [JsonPropertyName("year")] public AnidleCriteria<int> Year { get; set; } = new();
    [JsonPropertyName("source")] public AnidleCriteria<string> Source { get; set; } = new();
    [JsonPropertyName("genre")] public List<AnidleCriteria<string>> Genre { get; set; } = [];
    [JsonPropertyName("theme")] public List<AnidleCriteria<string>> Theme { get; set; } = [];
    [JsonPropertyName("studio")] public List<AnidleCriteria<string>> Studio { get; set; } = [];
    [JsonPropertyName("score")] public AnidleCriteria<string> Score { get; set; } = new();
}