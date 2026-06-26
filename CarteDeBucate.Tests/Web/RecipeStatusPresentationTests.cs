using CarteDeBucate.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public class RecipeStatusPresentationTests
{
    [Theory]
    [InlineData(RecipeStatus.Saved, "Salvată")]
    [InlineData(RecipeStatus.ToTry, "De încercat")]
    [InlineData(RecipeStatus.Tried, "Încercată")]
    [InlineData(RecipeStatus.Favorite, "Favorită")]
    [InlineData(RecipeStatus.NeedsChanges, "Necesită modificări")]
    [InlineData(RecipeStatus.DoNotRepeat, "Nu mai repet")]
    public void GetLabel_ShouldReturnRomanianLabel(
        RecipeStatus status,
        string expectedLabel)
    {
        string label = RecipeStatusPresentation.GetLabel(status);

        Assert.Equal(expectedLabel, label);
    }

    [Theory]
    [InlineData(RecipeStatus.Saved, "text-bg-secondary")]
    [InlineData(RecipeStatus.ToTry, "text-bg-info")]
    [InlineData(RecipeStatus.Tried, "text-bg-primary")]
    [InlineData(RecipeStatus.Favorite, "text-bg-warning")]
    [InlineData(RecipeStatus.NeedsChanges, "text-bg-dark")]
    [InlineData(RecipeStatus.DoNotRepeat, "text-bg-danger")]
    public void GetBadgeClass_ShouldReturnClassForEveryStatus(
        RecipeStatus status,
        string expectedClass)
    {
        string badgeClass = RecipeStatusPresentation.GetBadgeClass(status);

        Assert.Equal(expectedClass, badgeClass);
    }

    [Fact]
    public void GetSelectList_ShouldContainAllStatusesAndSelectCurrentStatus()
    {
        List<SelectListItem> items =
            RecipeStatusPresentation.GetSelectList(RecipeStatus.Favorite);

        Assert.Equal(Enum.GetValues<RecipeStatus>().Length, items.Count);
        SelectListItem selectedItem = Assert.Single(items, item => item.Selected);
        Assert.Equal(nameof(RecipeStatus.Favorite), selectedItem.Value);
        Assert.Equal("Favorită", selectedItem.Text);
    }
}
