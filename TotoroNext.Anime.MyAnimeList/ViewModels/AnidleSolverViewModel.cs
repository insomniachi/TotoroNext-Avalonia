using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList.ViewModels;

[UsedImplicitly]
public partial class AnidleSolverViewModel(IFactory<IMetadataService, Guid> metadataFactory) : ObservableObject, IAsyncInitializable
{
    private readonly IMetadataService _metadataService = metadataFactory.Create(Module.Id);

    [ObservableProperty] public partial string Query { get; set; } = "";
    [ObservableProperty] public partial string SelectedSuggestion { get; set; } = "";
    [ObservableProperty] public partial string Answer { get; set; } = "";
    [ObservableProperty] public partial DateTime? Date { get; set; } = new DateTime(2025, 2, 3);
    [ObservableProperty] public partial List<AnimeModel> PossibleAnswers { get; set; } = [];
    [ObservableProperty] public partial List<string> AutoCompleteData { get; set; } = [];
    [ObservableProperty] public partial bool AutoSolve { get; set; } = true;
    public ObservableCollection<AnidleAnswerResponse> AttemptedAnswers { get; } = [];
    public AnidleAggregatedAnswer AggregatedAnswer { get; } = new();
    public int Round { get; set; } = 1;
    public int ErrorCount { get; set; }

    public async Task InitializeAsync()
    {
        _ = await _metadataService.GetGenresAsync();
        var autoCompleteData = await "https://cms.aniguessr.com/wp-json/aniguessr/v1/autocomplete/anime"
            .GetJsonAsync<Dictionary<string, string>>();

        AutoCompleteData = autoCompleteData.Values.ToList();

        this.WhenAnyValue(x => x.SelectedSuggestion)
            .WhereNotNull()
            .Subscribe(suggestion =>
            {
                Query = "";
                Answer = suggestion;
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

        // Error
        if (response.Data.Year.Value == 0)
        {
            ErrorCount++;
            if (AutoSolve)
            {
                AutomaticGuess();
            }
            return Unit.Default;
        }

        ErrorCount = 0;
        response.Data.Title.IsValid = response.IsCorrect;

        AttemptedAnswers.Add(response);

        if (response.IsCorrect)
        {
            return Unit.Default;
        }

        AggregatedAnswer.UpdateGenres([..response.Data.Genre, ..response.Data.Theme]);
        AggregatedAnswer.UpdateYear(response.Data.Year);
        AggregatedAnswer.UpdateScore(response.Data.Score);

        PossibleAnswers = await _metadataService.SearchAnimeAsync(new AdvancedSearchRequest
        {
            IncludedGenres = [..AggregatedAnswer.CorrectGenres],
            ExcludedGenres = [..AggregatedAnswer.IncorrectGenres],
            MaximumScore = AggregatedAnswer.MaximumScore,
            MinimumScore = AggregatedAnswer.MinimumScore,
            MinYear = AggregatedAnswer.MinimumYear,
            MaxYear = AggregatedAnswer.MaximumYear
        });

        Round++;

        if (AutoSolve)
        {
            AutomaticGuess();
        }

        return Unit.Default;
    }

    private void AutomaticGuess()
    {
        AnimeModel? nextAnswer;
        do
        {
            nextAnswer = PossibleAnswers.Skip(ErrorCount).FirstOrDefault();
            if (nextAnswer is null)
            {
                return;
            }

            if (nextAnswer.Title == Answer)
            {
                ErrorCount++;
            }
            
        } while (nextAnswer.Title == Answer);

        _ = Task.Run(() => AttemptGuess(nextAnswer));
    }
}

public class AnidleCriteria
{
    [JsonPropertyName("valid")] public bool IsValid { get; set; }
    [JsonPropertyName("compare")] public string Compare { get; set; } = "";
}

public class AnidleCriteria<T> : AnidleCriteria
{
    [JsonPropertyName("value")] public T Value { get; set; } = default!;
}

[UsedImplicitly]
public class AnidleAnswerResponse
{
    [JsonPropertyName("response")] public bool IsCorrect { get; set; }
    [JsonPropertyName("data")] public AnidleAnswerResponseData Data { get; set; } = new();
}

public class AnidleAnswerResponseData
{
    [JsonPropertyName("title")] public AnidleCriteria<string> Title { get; set; } = new();
    [JsonPropertyName("year")] public AnidleCriteria<int> Year { get; set; } = new();
    [JsonPropertyName("source")] public AnidleCriteria<string> Source { get; set; } = new();
    [JsonPropertyName("genre")] public List<AnidleCriteria<string>> Genre { get; set; } = [];
    [JsonPropertyName("theme")] public List<AnidleCriteria<string>> Theme { get; set; } = [];
    [JsonPropertyName("studio")] public List<AnidleCriteria<string>> Studio { get; set; } = [];
    [JsonPropertyName("score")] public AnidleCriteria<string> Score { get; set; } = new();
}

public class AnidleAggregatedAnswer
{
    public HashSet<string> CorrectGenres { get; } = [];
    public HashSet<string> IncorrectGenres { get; } = [];
    public int? MinimumYear { get; set; }
    public int? MaximumYear { get; set; }
    public float? MinimumScore { get; set; }
    public float? MaximumScore { get; set; }

    public void UpdateGenres(IEnumerable<AnidleCriteria<string>> genresAndThemes)
    {
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
    }

    public void UpdateYear(AnidleCriteria<int> yearCriteria)
    {
        if (yearCriteria.IsValid)
        {
            MinimumYear = MaximumYear = yearCriteria.Value;
        }
        else
        {
            if (yearCriteria.Compare == ">")
            {
                MaximumYear = MaximumYear is not null
                    ? int.Min(MaximumYear.Value, yearCriteria.Value - 1)
                    : yearCriteria.Value - 1;
            }
            else
            {
                MinimumYear = MinimumYear is not null
                    ? int.Max(MinimumYear ?? int.MinValue, yearCriteria.Value + 1)
                    : yearCriteria.Value + 1;
            }
        }
    }

    public void UpdateScore(AnidleCriteria<string> scoreCriteria)
    {
        var score = float.Parse(scoreCriteria.Value);
        if (scoreCriteria.IsValid)
        {
            MinimumScore = MaximumScore = score;
        }
        else
        {
            if (scoreCriteria.Compare == ">")
            {
                MaximumScore = (float)Math.Round(MaximumScore is not null
                                                     ? float.Min(MaximumScore ?? float.MaxValue, score)
                                                     : score, 2);
            }
            else
            {
                MinimumScore = (float)Math.Round(MinimumScore is not null
                                                     ? float.Max(MinimumScore ?? float.MinValue, score)
                                                     : score, 2);
            }
        }
    }
}