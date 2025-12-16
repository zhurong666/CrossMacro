# Contributing to CrossMacro

First off, thanks for taking the time to contribute! 🎉

The following is a set of guidelines for contributing to CrossMacro. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

## 🐛 Reporting Bugs

This section guides you through submitting a bug report for CrossMacro.

- **Use the Bug Report template**: When you open a new issue, select "Bug Report".
- **Provide specific details**: Include your platform (Windows, Linux X11, or Wayland compositor), OS version, and steps to reproduce.
- **Include logs**: If possible, run the application from the terminal and include the output.

## 💡 Suggesting Enhancements

- **Use the Feature Request template**: Select "Feature Request" when opening an issue.
- **Explain the 'Why'**: Describe the problem you are trying to solve.

## 💻 Development Setup

1. **Prerequisites**:
   - .NET 10 SDK
   - **Linux**: `libevdev` and `uinput` permissions (see README for setup)
   - **Windows**: No additional requirements

2. **Build the project**:
   ```bash
   dotnet build
   ```

3. **Run the UI**:
   ```bash
   dotnet run --project src/CrossMacro.UI/
   ```

## 📥 Pull Requests

1. Fork the repo and create your branch from `main`.
2. Make sure your code follows the existing coding style.
3. Test your changes manually before submitting.
4. Open a Pull Request!

### PR Checks
We have a GitHub Action that automatically builds your PR. Make sure this check passes.

