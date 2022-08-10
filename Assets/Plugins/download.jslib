mergeInto(LibraryManager.library, {

    downloadToFile: function (content, filename) {
        const contentStr = Pointer_stringify(content);
        const filenameStr = Pointer_stringify(filename);

        const blob = new Blob([contentStr], { type: "application/json" });
        const url = URL.createObjectURL(blob, { oneTimeOnly: true });

        const element = document.createElement("a");
        element.href = url;
        element.download = filenameStr;
        element.style.display = "none";
        document.body.appendChild(element);

        element.click();

        document.body.removeChild(element);
    }

});