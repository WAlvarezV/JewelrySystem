using System.Runtime.CompilerServices;

// 2. Server
[assembly: InternalsVisibleTo("Pomona.Pwa.Server")]

// 3. Core
[assembly: InternalsVisibleTo("Pomona.Application")]

// 4. Infraestructure 
[assembly: InternalsVisibleTo("Pomona.Infrastructure")]