using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionLine.CodingChallenge
{
    public class SearchEngine
    {
        private readonly Dictionary<Guid, List<Shirt>> _sizewiseShirts;
        private readonly Dictionary<Guid, List<Shirt>> _colorwiseShirts;

        public SearchEngine(List<Shirt> shirts)
        {
            _sizewiseShirts = shirts.GroupBy(x => x.Size.Id).ToDictionary(x => x.Key, x => x.ToList());
            _colorwiseShirts = shirts.GroupBy(x => x.Color.Id).ToDictionary(x => x.Key, x => x.ToList());
        }

        public SearchResults Search(SearchOptions options)
        {
            List<Guid> searchColors = options.Colors.Any() ? options.Colors.Select(x => x.Id).ToList() : Color.All.Select(x => x.Id).ToList();
            var selectedColors = SearchTask(searchColors, _colorwiseShirts);

            List<Guid> searchSizes = options.Sizes.Any() ? options.Sizes.Select(x => x.Id).ToList() : Size.All.Select(x => x.Id).ToList();
            var selectedSizes = SearchTask(searchSizes, _sizewiseShirts);

            RunMultiple(selectedColors, selectedSizes);

            var sizecolorResults = selectedColors.Result.Intersect(selectedSizes.Result).ToList();
            return GetSearchResults(sizecolorResults);
        }

        private Task<List<Shirt>> SearchTask(List<Guid> criteria, Dictionary<Guid, List<Shirt>> selectFrom)
        {
            var searchResults = new List<Shirt>();
            foreach (var value in criteria)
            {
                searchResults.AddRange(selectFrom.Where(sws => sws.Key == value).Select(s => s.Value).FirstOrDefault());
            }
            return Task.FromResult(searchResults.ToList());
        }

        private void RunMultiple(params Task[] tasks)
        {
            Task.WhenAll(tasks);
            var failedTask = tasks.FirstOrDefault(x => x.IsFaulted);
            if (failedTask != null)
            {
                throw new Exception("One or more search tasks failed", failedTask.Exception);
            }
        }

        private SearchResults GetSearchResults(List<Shirt> sizecolorResults)
        {
            var colorSummary = GetColorCounts(sizecolorResults);

            var sizesSummary = GetSizeCounts(sizecolorResults);

            RunMultiple(colorSummary, sizesSummary);

            return new SearchResults
            {
                Shirts = sizecolorResults,
                ColorCounts = colorSummary.Result,
                SizeCounts = sizesSummary.Result
            };
        }

        private Task<List<ColorCount>> GetColorCounts(List<Shirt> coloredShirts)
        {
            var colorsSummary = coloredShirts
                .GroupBy(x => x.Color)
                .Select(c => new ColorCount()
                {
                    Color = c.Key,
                    Count = c.Count()
                }).ToList();
            colorsSummary.AddRange(Color.All.Except(colorsSummary.Select(x => x.Color)).Select(color => new ColorCount() { Color = color, Count = 0 }));
            return Task.FromResult(colorsSummary);
        }

        private Task<List<SizeCount>> GetSizeCounts(List<Shirt> sizedShirts)
        {
            var sizesSummary = sizedShirts
                .GroupBy(x => x.Size)
                .Select(c => new SizeCount()
                {
                    Size = c.Key,
                    Count = c.Count()
                }).ToList();

            sizesSummary.AddRange(Size.All.Except(sizesSummary.Select(x => x.Size)).Select(size => new SizeCount() { Size = size, Count = 0 }));
            return Task.FromResult(sizesSummary);
        }

    }
}