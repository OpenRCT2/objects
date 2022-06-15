#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import { spawn } from 'child_process';
import { platform } from 'os';

async function main() {
    // fs.cpSync('objects', 'artifacts', {recursive: true});
    const objects = await getObjects('artifacts');
    for (const obj of objects) {
        const images = obj.images;
	if (images === undefined) {
            continue;
	}
	var requiresProcessing = true;
        if (typeof images[Symbol.iterator] === 'function') {
	    for (const image of images) {
                if (typeof image === 'string') {
                    requiresProcessing = false;
		}    
	    }
        }
	if (requiresProcessing) {
            console.log(`Reprocess ${obj.id}`);
	}
    }
}

async function getObjects(dir) {
    const result = [];
    const files = await getAllFiles(dir);
    for (const file of files) {
        const jsonRegex = /^.+\..+\.json$/;
        if (jsonRegex.test(file)) {
            const cwd = path.join('..', path.dirname(file));
            const obj = await readJsonFile(file);
            obj.cwd = cwd;
            result.push(obj);
        }
    }
    return result;
}

function readJsonFile(path) {
    return new Promise((resolve, reject) => {
        fs.readFile(path, 'utf8', (err, data) => {
            if (err) {
                reject(err);
            } else {
                resolve(JSON.parse(data));
            }
        });
    });
}

function writeJsonFile(path, data) {
    return new Promise((resolve, reject) => {
        const json = JSON.stringify(data, null, 4) + '\n';
        fs.writeFile(path, json, 'utf8', err => {
            if (err) {
                reject(err);
            } else {
                resolve();
            }
        });
    });
}

function getAllFiles(root) {
    return new Promise((resolve, reject) => {
        const results = [];
        let pending = 0;
        const find = (root) => {
            pending++;
            fs.readdir(root, (err, fileNames) => {
                // if (err) {
                //     reject(err);
                // }
                for (const fileName of fileNames) {
                    const fullPath = path.join(root, fileName);
                    pending++;
                    fs.stat(fullPath, (err, stat) => {
                        // if (err) {
                        //     reject(err);
                        // }
                        if (stat) {
                            if (stat.isDirectory()) {
                                find(fullPath);
                            } else {
                                results.push(fullPath);
                            }
                        }
                        pending--;
                        if (pending === 0) {
                            resolve(results);
                        }
                    });
                }
                pending--;
                if (pending === 0) {
                    resolve(results.sort());
                }
            });
        };
        find(root);
    });
}

try {
    await main();
} catch (err) {
    console.log(err.message);
    process.exitCode = 1;
}
