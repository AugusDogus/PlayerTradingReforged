using System.Security.Permissions;

// Allow Mono to call game members exposed by the build-time assembly publicizer.
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
