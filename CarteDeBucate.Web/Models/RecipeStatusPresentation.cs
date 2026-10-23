using Microsoft.AspNetCore.Mvc.Rendering;

namespace CarteDeBucate.Web.Models;

public static class RecipeStatusPresentation
{
    public static string GetLabel(RecipeStatus status)
    {
        return status switch
        {
            RecipeStatus.Saved => "Salvată",
            RecipeStatus.ToTry => "De încercat",
            RecipeStatus.Tried => "Încercată",
            RecipeStatus.Favorite => "Favorită",
            RecipeStatus.NeedsChanges => "Necesită modificări",
            RecipeStatus.DoNotRepeat => "Nu mai repet",
            _ => status.ToString()
        };
    }

    public static string GetBadgeClass(RecipeStatus status)
    {
        return status switch
        {
            RecipeStatus.Saved => "text-bg-secondary",
            RecipeStatus.ToTry => "text-bg-info",
            RecipeStatus.Tried => "text-bg-primary",
            RecipeStatus.Favorite => "text-bg-warning",
            RecipeStatus.NeedsChanges => "text-bg-dark",
            RecipeStatus.DoNotRepeat => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }

    public static List<SelectListItem> GetSelectList(RecipeStatus selectedStatus)
    {
        return Enum.GetValues<RecipeStatus>()
            .Select(status => new SelectListItem
            {
                Text = GetLabel(status),
                Value = status.ToString(),
                Selected = status == selectedStatus
            })
            .ToList();
    }
}
