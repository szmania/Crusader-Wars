import os
from aider.tools.base_tool import BaseAiderTool

class CK3StockFileSearchTool(BaseAiderTool):
    """
    A tool to search through stock CK3 game files for specific strings.
    This tool helps in finding examples of GUI function calls and other
    implementations within the stock game files.
    """
    BASE_PATH = "F:\\Program Files\\Paradox Interactive\\testing_ck3_stock_files\\game"
    VALID_SUBDIRECTORIES = ["common", "gui", "localization"]

    def __init__(self, coder, **kwargs):
        super().__init__(coder, **kwargs)

    def get_tool_definition(self):
        return {
            "type": "function",
            "function": {
                "name": "CK3StockFileSearchTool",
                "description": (
                    f"Searches through the stock CK3 game files in '{self.BASE_PATH}' for a given string. "
                    "This is useful for finding how to use specific GUI functions or other game features."
                ),
                "parameters": {
                    "type": "object",
                    "properties": {
                        "query": {
                            "type": "string",
                            "description": "The text string to search for in the files (e.g., a function name, a variable).",
                        },
                        "subdirectories": {
                            "type": "array",
                            "items": {
                                "type": "string",
                                "enum": self.VALID_SUBDIRECTORIES,
                            },
                            "description": (
                                "A list of subdirectories to search within. If not provided, all valid directories will be searched: "
                                f"{', '.join(self.VALID_SUBDIRECTORIES)}."
                            ),
                        },
                        "file_extensions": {
                            "type": "array",
                            "items": {
                                "type": "string",
                            },
                            "description": "A list of file extensions to limit the search to (e.g., ['.gui', '.txt', '.yml']). If not provided, all files are searched.",
                        },
                    },
                    "required": ["query"],
                },
            },
        }

    def run(self, query, subdirectories=None, file_extensions=None):
        """
        Executes the search for the given query in the specified CK3 stock files.

        :param query: The string to search for.
        :param subdirectories: Optional list of subdirectories to search in.
        :param file_extensions: Optional list of file extensions to filter by.
        :return: A formatted string of search results or an error/not found message.
        """
        if not os.path.isdir(self.BASE_PATH):
            error_msg = f"Error: The base directory '{self.BASE_PATH}' does not exist. Please check the path."
            self.coder.io.tool_error(error_msg)
            return error_msg

        if subdirectories is None or not subdirectories:
            search_dirs = self.VALID_SUBDIRECTORIES
        else:
            # Validate provided subdirectories
            search_dirs = [d for d in subdirectories if d in self.VALID_SUBDIRECTORIES]
            if not search_dirs:
                return f"Error: None of the provided subdirectories are valid. Please choose from: {', '.join(self.VALID_SUBDIRECTORIES)}"

        results = []
        self.coder.io.tool_output(f"Starting search for '{query}' in '{self.BASE_PATH}'...")

        for subdir in search_dirs:
            search_path = os.path.join(self.BASE_PATH, subdir)
            if not os.path.isdir(search_path):
                continue

            for root, _, files in os.walk(search_path):
                for file in files:
                    if file_extensions:
                        # Ensure extensions start with a dot for comparison
                        normalized_extensions = [ext if ext.startswith('.') else f'.{ext}' for ext in file_extensions]
                        if not any(file.endswith(ext) for ext in normalized_extensions):
                            continue
                    
                    file_path = os.path.join(root, file)
                    try:
                        with open(file_path, 'r', encoding='utf-8-sig', errors='ignore') as f:
                            for line_num, line in enumerate(f, 1):
                                if query in line:
                                    relative_path = os.path.relpath(file_path, self.BASE_PATH)
                                    results.append(f"{relative_path}:{line_num}: {line.strip()}")
                    except Exception as e:
                        # Log error for a specific file but continue searching
                        self.coder.io.tool_error(f"Could not read file {file_path}: {e}")

        if not results:
            return f"No results found for '{query}'."

        # Format the output
        output = f"Found {len(results)} results for '{query}':\n"
        output += "\n".join(results)
        return output
