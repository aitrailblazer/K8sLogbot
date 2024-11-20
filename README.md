# K8sLogbot: Kubernetes Log Analyzer

K8sLogbot is a GitHub Action workflow that utilizes a C# application to analyze logs from an Azure Kubernetes Service (AKS) cluster. It provides insights and alerts on detected issues by leveraging AI-powered analysis.

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

K8sLogbot automates Kubernetes log analysis within the GitHub ecosystem. Here's what it does:

- **Fetches Logs**: Connects to your AKS cluster to retrieve pod logs.
- **Analyzes Logs**: Uses AI to detect errors, warnings, or patterns indicative of issues.
- **Reports Findings**: Posts analysis results as comments in GitHub Issues for collaborative review and action.

## Prerequisites

Before using K8sLogbot, ensure you have:

- An Azure Kubernetes Service (AKS) cluster.
- A GitHub repository.
- `kubectl` installed and configured to access your AKS cluster.
- GitHub Actions enabled in your repository.
- Basic understanding of GitHub Actions, Kubernetes, and C#.
- Access to OpenAI or Azure OpenAI services.

## Setup

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/yourusername/K8sLogbot.git
   cd K8sLogbot
Configure GitHub Secrets:

KUBECONFIG: Add your kubeconfig file content as a secret in your GitHub repository settings.

Navigate to Settings > Secrets and variables > Actions in your GitHub repository.
Click New repository secret.
Name it KUBECONFIG and paste your kubeconfig content.
Click Add secret.
ACCESS_CODE_HASH: Compute the SHA256 hash of your access code.

echo -n "your_access_code" | sha256sum
Set the resulting hash as a secret named ACCESS_CODE_HASH.

ENDPOINT, API_KEY, MODEL: Set these for OpenAI service access.

ENDPOINT: Your OpenAI API endpoint.
API_KEY: Your OpenAI API key.
MODEL: The model name (e.g., gpt-4, gpt-3.5-turbo).
Install Dependencies:

Ensure you have the .NET SDK installed. Then, restore the dependencies:

dotnet restore
Build the Application:

dotnet build
Set Environment Variables:

Set the following environment variables in your shell or as GitHub secrets:

export ACCESS_CODE_HASH="your_sha256_hash_of_access_code"
export ENDPOINT="https://api.openai.com/v1/"
export API_KEY="your_api_key"
export MODEL="gpt-3.5-turbo"
Usage
Running the Analysis
Run the application by providing your access code:

dotnet run -- "your_access_code"
Reviewing Results
Once the analysis is complete, review the results posted as comments in the relevant GitHub Issues for further action.

Workflow Configuration
Configure the workflow to suit your needs by modifying the GitHub Actions YAML files in the .github/workflows directory.

Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.