---
layout: page
title: RailEssentials 
permalink: /
---

# Welcome to RailEssentials

This is a starter template for a docsy jekyll theme.

![assets/img/docsy-jekyll.png](assets/img/docsy-jekyll.png)

## Purpose

GitHub pages uses Jekyll natively, so when I make documentation, I typically
look for Jekyll templates. Why? Using Jekyll means that I can use markdown,
and allow for users to easily contribute, and build automatically just by
way of pushing to a master branch (or general GitHub pages).
I found Docsy, a beautiful Hugo template, but it requires hugo with GoLang
which doesn't render natively on GitHub pages. For this reason, I've spent
some time creating a custom Jekyll template that is (almost) as beautiful,
and includes all the features that I might want.

## Features

What are these features? You should see the {% include doc.html name="Getting Started" path="getting-started" %}
guide for a complete summary. Briefly:

 - *User interaction* including consistent permalinks, links to ask questions via GitHub issues, and edit the file on GitHub directly.
 - *Search* across posts, documentation, and other site pages, with an ability to exclude from search.
 - *External Search* meaning an ability to link any page tag to trigger an external search.
 - *Documentation* A documentation collection that was easy to organize on the filesystem, render with nested headings for the user, and refer to in markdown.
 - *Pages* A separate folder for more traditional pages (e.g, about).
 - *Navigation*: Control over the main navigation on the left of the page, and automatic generation of table of contents for each page on the right.
 - *News* A posts feed for news and updates, along with an archive (organized by year).
 - *Templates* or specifically, "includes" that make it easy to create an alert, documentation link, or other content.
 - *Continuous Integration* recipes to preview the site


For features, getting started with development, see the {% include doc.html name="Getting Started" path="getting-started" %} page. Would you like to request a feature or contribute?
[Open an issue]({{ site.repo }}/issues)
