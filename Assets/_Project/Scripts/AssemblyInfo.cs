using System.Runtime.CompilerServices;

// Разрешает PlayMode-тестам обращаться к internal-членам Runtime-сборки.
[assembly: InternalsVisibleTo("DiplomaGame.Tests.Runtime")]
