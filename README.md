# Project Raven (Karasu)
____
Project Raven, otherwise known as Karasu (Japanese for Raven), is a Discord bot written in C# using [Discord.NET  2.0.1](https://github.com/discord-net/Discord.Net), built against [.NET Core 2.2](https://github.com/dotnet/core). Raven attempts to simply the user/admin experience by simplifying the entire setup process with a dynamic configuration menu.

In addition to this, it offers a number of helpful moderation and management functions for Discord servers, with almost all commands being toggleable at an individual or category (module) level.

____
## Why Raven?

The name Raven was given due to the fact it uses RavenDB for its persistent data store and it made for a convenient project/bot name. It was given the sub-name of Karasu due to Discord actually having 10000 users registered under the name Raven, preventing us using that name for the bot. My brother lives in Japan, so I asked him what Raven was in Japanese - and that's how I arrived at the name.

____
## What makes this bot different from all the other Discord Bots that exist?

An interesting question. There are by no means a shortage of Discord bots that exist. However, Raven does a few things differently. The aim of Raven was to make it as extensible as possible, and it achives this through the a dynamic plugin system. When the bot is run, it loads as many plugins as it finds, and if they are in the right format, it'll load all commands and event listeners it finds.

This allows you to effectivly use the base framework of Raven to write a bot on top of it. The framework still handles a number of features and overall basic functioanality.

(a plugin tutorial will be made when the bot is considered finished, as long with a feature list) 

____
### A Few Notes

Before I go any further, note this bot is under active development, so not everything mentioned is fully functional or implemented as of this moment. The bot is licensed under GPL3.0 so you are free to do what ever you want with it, but you are obligated to share any changes you make.

____
## To Finish Later

That's enough of a readme for now Laz. Back to work.
