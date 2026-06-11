using System.Runtime.CompilerServices;

// Разрешает PlayMode- и EditMode-тестам обращаться к internal-членам Runtime-сборки.
[assembly: InternalsVisibleTo("DiplomaGame.Tests.Runtime")]
[assembly: InternalsVisibleTo("DiplomaGame.Tests.Editor")]
