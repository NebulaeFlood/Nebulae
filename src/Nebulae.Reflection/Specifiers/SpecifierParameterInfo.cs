using System;
using System.Reflection;

namespace Nebulae.Reflection.Specifiers
{
    internal sealed class SpecifierParameterInfo(string name, Type parameterType) : ParameterInfo
    {
        public override string? Name => name;

        public override Type ParameterType => parameterType;
    }
}
