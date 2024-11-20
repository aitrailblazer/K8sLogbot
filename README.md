# K8sLogbot: Kubernetes Log Analyzer

**K8sLogbot** is a GitHub Action workflow that utilizes a C# application to analyze logs from an Azure Kubernetes Service (AKS) cluster, providing insights and alerting on detected issues.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
- [Usage](#usage)
  - [Running the Analysis](#running-the-analysis)
  - [Reviewing Results](#reviewing-results)
- [Workflow Configuration](#workflow-configuration)
- [Contributing](#contributing)
- [License](#license)

## Overview

This project aims to automate the process of Kubernetes log analysis within the GitHub ecosystem. Here's what **K8sLogbot** does:

- **Fetches Logs:** Connects to your AKS cluster to retrieve pod logs.
- **Analyzes Logs:** Performs analysis to detect errors, warnings, or patterns indicative of issues.
- **Reports Findings:** Posts analysis results as comments in GitHub Issues for collaborative review and action.

## Prerequisites

Before you start using **K8sLogbot**, ensure you have:

- An Azure Kubernetes Service (AKS) cluster.
- GitHub repository setup.
- `kubectl` installed and configured to access your AKS cluster.
- A GitHub Actions enabled repository.
- Basic understanding of GitHub Actions, Kubernetes, and C#.

## Setup

1. **Clone the Repository:**
Configure GitHub Secrets:
KUBECONFIG: Add your kubeconfig file content as a secret in GitHub repository settings under Settings > Secrets and variables > Actions.
Install Dependencies:

Install Dependencies:
bash
dotnet restore

Usage
Running the Analysis
Manual Trigger:
Navigate to the Actions tab in your repository on GitHub.
Click on K8sLogbot workflow.
Select Run workflow. You can choose the environment if inputs are configured.
Scheduled or Event-Triggered:
Ensure the workflow is set up with cron schedules or event triggers in the workflow file.

Reviewing Results
Analysis results will be commented on a GitHub issue. You can specify the issue number in the workflow or create one dynamically.

Workflow Configuration
Here's how you can configure your GitHub Actions workflow:

```yaml
name: K8sLogbot

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'The environment to analyze logs from'
        required: true
        default: 'development'
        type: choice
        options:
        - development
        - staging
        - production

jobs:
  analyze-logs:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout code
      uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '6.0.x'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Run Log Analysis
      env:
        KUBECONFIG: ${{ secrets.KUBECONFIG }}
        ENVIRONMENT: ${{ github.event.inputs.environment }}
      run: dotnet run -- --environment $ENVIRONMENT
    - name: Post Analysis to GitHub Issue
      if: always()
      uses: actions/github-script@v3
      with:
        github-token: ${{secrets.GITHUB_TOKEN}}
        script: |
          const fs = require('fs');
          const logs = fs.readFileSync('logs.txt', 'utf8');
          github.issues.createComment({
            issue_number: 1, // Replace with your issue number or logic to find/create issue
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: 'Analysis results:\n\n' + logs
          });
```
