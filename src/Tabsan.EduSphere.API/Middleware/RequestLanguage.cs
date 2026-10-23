using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tabsan.EduSphere.Application.Localization;

namespace Tabsan.EduSphere.API.Middleware;

// Plan M11 — the API answers in the caller's language (Accept-Language "ur" / "ar", forwarded by the portal).

/// <summary>
/// Sets the UI culture (the language of messages, validation errors and PDF labels) from Accept-Language. The formatting
/// culture is left alone, so numbers and dates in exports, imports and logs are unaffected.
/// </summary>
public sealed class RequestLanguageMiddleware
{
    private static readonly ConcurrentDictionary<string, CultureInfo> Cultures = new();
    private readonly RequestDelegate _next;

    public RequestLanguageMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var lang = PortalLanguages.FromAcceptLanguage(context.Request.Headers.AcceptLanguage);
        if (lang is null || lang == PortalLanguages.English)
        {
            await _next(context);
            return;
        }

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = Cultures.GetOrAdd(lang, l => CultureInfo.ReadOnly(new CultureInfo(l == "ur" ? "ur-PK" : "ar-SA")));
        context.Response.Headers.ContentLanguage = lang;
        try { await _next(context); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
}

/// <summary>
/// Translates the text of API answers — <c>{ message }</c> objects, problem details and validation errors — into the
/// request's language. Answers without a translation stay in English; English requests are not touched.
/// </summary>
public sealed class LocalizedResponseFilter : IAsyncAlwaysRunResultFilter
{
    private static readonly string[] TextProperties = ["message", "error", "detail", "title"];
    private readonly ITextLocalizer _text;

    public LocalizedResponseFilter(ITextLocalizer text) => _text = text;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (PortalLanguages.Current != PortalLanguages.English && context.Result is ObjectResult { Value: { } value } result)
            result.Value = Localize(value);
        await next();
    }

    private object Localize(object value)
    {
        switch (value)
        {
            case ValidationProblemDetails validation:
                foreach (var key in validation.Errors.Keys.ToList())
                    validation.Errors[key] = validation.Errors[key].Select(e => _text.Translate(e)).ToArray();
                validation.Title = Translate(validation.Title);
                validation.Detail = Translate(validation.Detail);
                return validation;
            case ProblemDetails problem:
                problem.Title = Translate(problem.Title);
                problem.Detail = Translate(problem.Detail);
                return problem;
        }

        var type = value.GetType();
        if (!IsAnonymous(type)) return value;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (!properties.Any(p => p.PropertyType == typeof(string) && TextProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
            return value;

        // Same JSON as before (camelCase names, nulls left out), with the text translated.
        var copy = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            var item = property.GetValue(value);
            if (item is null) continue;
            if (item is string text && TextProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) item = _text.Translate(text);
            copy[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = item;
        }
        return copy;
    }

    private string? Translate(string? text) => text is null ? null : _text.Translate(text);

    private static bool IsAnonymous(Type type)
        => type.Name.Contains("AnonymousType", StringComparison.Ordinal) && type.IsGenericType
           && type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null;
}
