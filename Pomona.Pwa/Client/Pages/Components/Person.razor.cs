using Microsoft.AspNetCore.Components;
using Pomona.Models.Models;
using Pomona.Pwa.Client.Custom;

namespace Pomona.Pwa.Client.Pages.Components
{
    public class PersonBase : CustomComponentBase
    {
        [Parameter] public PersonModel Person { get; set; } = new();
    }
}
