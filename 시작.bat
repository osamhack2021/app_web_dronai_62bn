@echo off
title Super awesome program
set CURPATH=%cd%
cd %CURPATH%\WEB\dronai-api
start yarn dev
cd %CURPATH%\WEB\dronai-dashboard
start yarn start