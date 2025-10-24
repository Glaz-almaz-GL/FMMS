using Huskui.Avalonia.Controls;

namespace FMMS.Models
{
    public partial class YesNoDialog : Dialog
    {

        protected override bool ValidateResult(object? result)
        {
            return true;
        }
    }
}
