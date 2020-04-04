namespace GogApi.DotNet.FSharp

open GogApi.DotNet.FSharp.Types

open FSharp.Json
open FsHttp
open FsHttp.DslCE

/// <summary>
/// This module contains low-level functions and types to make requests to the GOG API
/// </summary>
module Request =
    /// <summary>
    /// Simple record for request parameters
    /// </summary>
    type RequestParameter =
        { name: string
          value: string }

    /// <summary>
    /// Creates a simple Request Parameter
    /// </summary>
    let createRequestParameter name value =
        { name = name
          value = value }

    /// <summary>
    /// Creates the GET request with correct authentication headers and parameters to given url
    /// </summary>
    /// <returns>
    /// An Async which can be executed to send the request
    /// </returns>
    let setupRequest auth queries url =
        // Add parameters to request url
        let url =
            match queries with
            | [] -> url
            | queries ->
                let parameters =
                    queries
                    |> List.map (fun param -> param.name + "=" + param.value)
                    |> List.reduce (fun param1 param2 -> param1 + "&" + param2)
                url + "?" + parameters
        // Headerpart which is always used - with authentication and without it
        let baseHeader =
            httpLazy {
                GET url
                CacheControl "no-cache"
            }
        // Extend request header with authentication info if available
        let request =
            match auth with
            | NoAuth -> baseHeader
            | Auth { accessToken = token } -> httpRequest baseHeader { BearerAuth token }

        request |> sendAsync

    /// <summary>
    /// Helper function which catches exception from FSharp.Json and returns Result type
    /// </summary>
    /// <returns>
    /// - Error when exception occured
    /// - otherwise Ok with parsed object
    /// </returns>
    let private parseJson<'T> rawJson =
        let parsedJson =
            try
                Json.deserialize<'T> rawJson |> Ok
            with ex -> Error(rawJson, ex.Message)
        parsedJson

    /// <summary>
    /// Function which creates an request which will be parsed into an object after returning
    /// </summary>
    /// <returns>
    /// An Async which can be executed to send the request and parse the answer
    /// </returns>
    let makeRequest<'T> auth queries url =
        async {
            let! response = setupRequest auth queries url

            let message = response |> toText
            return parseJson<'T> message
        }
