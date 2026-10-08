# VitalReach coding standards

## Blazor component separation

- Keep `.razor` files focused on Razor directives and markup.
- Put injected services, parameters, component state, lifecycle methods, event handlers, query-string properties, and other C# implementation in the matching `.razor.cs` partial class.
- Do not add inline `@code` blocks, including empty blocks.
- Use the namespace implied by the component path (`VitalReach.Web.Components` or `VitalReach.Web.Components.Pages`).
- Enable nullable reference types in code-behind files and keep user-facing success and error messages explicit.

## Local configuration

- Keep local-only settings in the ignored `.env` file and document supported keys in `.env.example`.
- Never commit credentials or other secrets. Environment variables supplied by the host take precedence over `.env` values.
