using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SizeOnDisk.ViewModel
{
    public class FileNameValidationRule : ValidationRule
    {
        public FileNameValidationRule()
        {
        }

        public FileNameValidationRule(ValidationStep validationStep, bool validatesOnTargetUpdated) : base(validationStep, validatesOnTargetUpdated)
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}
