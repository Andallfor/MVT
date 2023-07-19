mergeInto(LibraryManager.library, 
{
    BrowserTextDownload: function(filename, textContent)
    {
        // https://ourcodeworld.com/articles/read/189/how-to-create-a-file-and-generate-a-download-with-javascript-in-the-browser-without-a-server
        
        // Convert paramters to the correct form. See Unity WebGL Plugins page
        // for more information. It's not too important to realize why you need 
        // to do this, as long as you know THAT you need to.
        var strFilename = Pointer_stringify(filename);
        var strContent = Pointer_stringify(textContent);

        // Create the hyperlink for a user to click
        var element = document.createElement('a');
        
        // Set the link destination as hard-coded file data.
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(strContent));
        element.setAttribute('download', strFilename);
        
        // Make sure it's not visible when added to the HTML body
        element.style.display = 'none'; 
        
        // Activate it by adding it to the HTML body
        document.body.appendChild(element);
        // Don't wait for the user to click it, activate it ourselves!
        element.click();
        // Clean up our mess, now that the anchor's purpose is finished.
        document.body.removeChild(element);
    },
});