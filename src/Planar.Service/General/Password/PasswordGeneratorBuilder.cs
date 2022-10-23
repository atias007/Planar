namespace Planar.Service.General.Password
{
    public class PasswordGeneratorBuilder
    {
        private bool _includeLowercase;
        private bool _includeUppercase;
        private bool _includeNumeric;
        private bool _includeSpecial;
        private bool _includeSpaces;
        private int _length = 6;

        public PasswordGeneratorBuilder IncludeLowercase()
        {
            _includeLowercase = true;
            return this;
        }

        public PasswordGeneratorBuilder IncludeUppercase()
        {
            _includeUppercase = true;
            return this;
        }

        public PasswordGeneratorBuilder IncludeNumeric()
        {
            _includeNumeric = true;
            return this;
        }

        public PasswordGeneratorBuilder IncludeSpecial()
        {
            _includeSpecial = true;
            return this;
        }

        public PasswordGeneratorBuilder IncludeSpaces()
        {
            _includeSpaces = true;
            return this;
        }

        public PasswordGeneratorBuilder WithLength(int length)
        {
            _length = length;
            return this;
        }

        public IPasswordGeneratorProperties Build()
        {
            var result = new PasswordGeneratorProperties
            {
                IncludeLowercase = _includeLowercase,
                IncludeNumeric = _includeNumeric,
                IncludeSpaces = _includeSpaces,
                IncludeSpecial = _includeSpecial,
                IncludeUppercase = _includeUppercase,
                Length = _length
            };

            return result;
        }
    }
}