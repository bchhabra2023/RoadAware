import { useState, useRef } from 'react';
import './App.css';

function App() {
    const [files, setFiles] = useState([]);
    const [userMessage, setUserMessage] = useState('');
    const [messages, setMessages] = useState([]);
    const [uploading, setUploading] = useState(false);
    const [uploadStatus, setUploadStatus] = useState('');
    const [streamedContent, setStreamedContent] = useState('');
    const streamedContentRef = useRef('');

    const handleFileChange = (e) => {
        setFiles(Array.from(e.target.files));
    };

    const handleTextChange = (e) => {
        setUserMessage(e.target.value);
    };

    const handleUpload = async (e) => {
        e.preventDefault();
        if (files.length === 0 && !userMessage.trim()) {
            setUploadStatus('Please enter a message or select at least one file');
            return;
        }
        setUploading(true);
        setUploadStatus('Analyzing...');
        setStreamedContent('');
        streamedContentRef.current = '';

        // Add user message immediately
        setMessages(prev => [
            ...prev,
            {
                role: 'user',
                text: userMessage,
                images: files.map(file => URL.createObjectURL(file)),
                filenames: files.map(file => file.name)
            }
        ]);

        try {
            const formData = new FormData();
            files.forEach(file => {
                formData.append('files', file);
            });
            formData.append('userMessage', userMessage);

            // Use the streaming endpoint
            const response = await fetch('https://localhost:7153/api/file/analyze-potholes/stream', {
                method: 'POST',
                body: formData,
            });

            if (!response.body) throw new Error('No response body');

            const reader = response.body.getReader();
            const decoder = new TextDecoder('utf-8');
            let done = false;
            let fullContent = '';

            while (!done) {
                const { value, done: doneReading } = await reader.read();
                done = doneReading;
                if (value) {
                    const chunk = decoder.decode(value);
                    fullContent += chunk;
                    streamedContentRef.current += chunk;
                    setStreamedContent(streamedContentRef.current);
                }
            }

            setMessages(prev => [
                ...prev,
                { role: 'assistant', content: fullContent }
            ]);
            setUploadStatus('');
            setFiles([]);
            setUserMessage('');
            setStreamedContent('');
        } catch (error) {
            setUploadStatus('Failed to analyze images: ' + error.message);
        } finally {
            setUploading(false);
        }
    };

    return (
        <div className="chat-container">
            <div className="landing-header">
                <img src="roadaware-logo.svg" alt="RoadAware Logo" className="landing-logo" />
                <h1 className="chat-title">RoadAware</h1>
                <div className="landing-subtitle">Analyze and prioritize road repairs using AI-powered image and text analysis</div>
            </div>
            <div className="chat-window">
                {messages.length === 0 && !streamedContent && (
                    <div className="chat-empty landing-desc">
                        Upload pothole or road images and describe the issue to get instant AI-powered analysis and repair prioritization.
                    </div>
                )}
                {messages.map((msg, idx) => (
                    <div key={idx} className={`chat-bubble ${msg.role}`}>
                        {msg.role === 'user' && (
                            <>
                                {msg.text && <div className="chat-user-text">{msg.text}</div>}
                                {msg.images && (
                                    <div className="chat-images">
                                        {msg.images.map((img, i) => (
                                            <div key={i} className="chat-image-wrapper">
                                                <img src={img} alt={msg.filenames[i]} className="chat-image" />
                                                <div className="chat-image-label">{msg.filenames[i]}</div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </>
                        )}
                        {msg.role === 'assistant' && (
                            <div className="analysis-result">
                                {msg.content && msg.content.includes('<table') ? (
                                    <div className="analysis-html-table" dangerouslySetInnerHTML={{ __html: msg.content }} />
                                ) : (
                                    <div className="chat-text">{msg.content}</div>
                                )}
                            </div>
                        )}
                    </div>
                ))}
                {/* Show streamed content as it arrives */}
                {streamedContent && (
                    <div className="chat-bubble assistant">
                        <div className="analysis-result">
                            {streamedContent.includes('<table') ? (
                                <div className="analysis-html-table" dangerouslySetInnerHTML={{ __html: streamedContent }} />
                            ) : (
                                <div className="chat-text">{streamedContent}</div>
                            )}
                        </div>
                    </div>
                )}
                {uploadStatus && (
                    <div className="chat-bubble assistant">
                        <div className="chat-text">{uploadStatus}</div>
                    </div>
                )}
            </div>
            <form className="chat-input-bar" onSubmit={handleUpload}>
                <input
                    type="text"
                    value={userMessage}
                    onChange={handleTextChange}
                    placeholder="Type your message..."
                    className="chat-text-input"
                    disabled={uploading}
                />
                <input
                    type="file"
                    multiple
                    accept="image/*"
                    onChange={handleFileChange}
                    disabled={uploading}
                    className="chat-file-input"
                />
                <button
                    type="submit"
                    disabled={uploading || (files.length === 0 && !userMessage.trim())}
                    className="chat-send-btn"
                >
                    {uploading ? 'Analyzing...' : 'Analyze'}
                </button>
            </form>
        </div>
    );
}

export default App;